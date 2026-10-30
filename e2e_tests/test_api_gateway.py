#!/usr/bin/env python3
"""
ShuffleSeries - API Gateway Comprehensive End-to-End Test Suite
Tests all API Gateway edge capabilities, routing, resilience, and security (http://localhost:5001):
1. Health & Readiness Probes (Gateway, Catalog, Identity)
2. YARP Reverse Proxy Routing & OpenAPI Aggregation
3. Distributed Tracing & Correlation ID Propagation (X-Correlation-ID)
4. Centralized CORS (Cross-Origin Resource Sharing) Preflight & Origin Handling
5. Gateway Edge Authentication & RBAC Policy Enforcement (RequirePremiumRole)
6. Redis Token & User Session Blacklist Enforcement at Edge (AUTH_TOKEN_REVOKED)
7. Gateway Rate Limiting & Throttling (StrictLimiter 429 Too Many Requests)
"""

import urllib.request
import urllib.error
import json
import time
import base64
import hmac
import hashlib
import uuid
import sys
import subprocess
import socket

GATEWAY_BASE_URL = "http://localhost:5001"
GATEWAY_HOST = "localhost"
GATEWAY_PORT = 5001

# ANSI Color Codes
GREEN = "\033[92m"
RED = "\033[91m"
YELLOW = "\033[93m"
BLUE = "\033[94m"
CYAN = "\033[96m"
BOLD = "\033[1m"
RESET = "\033[0m"

total_tests = 0
passed_tests = 0
failed_tests = 0

JWT_SECRET = "SuperSecretSecretKeyForJwtAuthenticationTokens2026!!"
JWT_ISSUER = "ShuffleSeries.Identity"
JWT_AUDIENCE = "ShuffleSeries.Microservices"
REDIS_CONTAINER = "shuffleseries_redis"
REDIS_PASSWORD = "SuperSecretRedisPassword2026!!"


def b64url(data: bytes) -> str:
    return base64.urlsafe_b64encode(data).rstrip(b"=").decode("utf-8")


def generate_jwt(user_id=None, email=None, roles=None, permissions=None, exp_seconds=3600, nbf_seconds=0, secret=JWT_SECRET, jti=None, iat_offset=0):
    now = int(time.time())
    header = {"alg": "HS256", "typ": "JWT"}
    payload = {
        "iss": JWT_ISSUER,
        "aud": JWT_AUDIENCE,
        "sub": user_id or str(uuid.uuid4()),
        "jti": jti or str(uuid.uuid4()),
        "email": email or "user@shuffleseries.com",
        "role": roles or ["Standard"],
        "permission": permissions or ["catalog:platforms:read"],
        "nbf": now + nbf_seconds,
        "iat": now + iat_offset,
        "exp": now + exp_seconds
    }
    segments = [
        b64url(json.dumps(header).encode("utf-8")),
        b64url(json.dumps(payload).encode("utf-8"))
    ]
    signing_input = ".".join(segments).encode("ascii")
    signature = hmac.new(secret.encode("utf-8"), signing_input, hashlib.sha256).digest()
    return f"{segments[0]}.{segments[1]}.{b64url(signature)}"


def redis_cli_set(key: str, value: str, ttl_seconds: int = 60):
    cmd = [
        "docker", "exec", REDIS_CONTAINER,
        "redis-cli", "-a", REDIS_PASSWORD,
        "SET", key, value, "EX", str(ttl_seconds)
    ]
    subprocess.run(cmd, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


def redis_cli_del(key: str):
    cmd = [
        "docker", "exec", REDIS_CONTAINER,
        "redis-cli", "-a", REDIS_PASSWORD,
        "DEL", key
    ]
    subprocess.run(cmd, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


def make_request(method, path, body=None, token=None, headers=None, pace=0.0):
    if pace > 0:
        time.sleep(pace)

    url = f"{GATEWAY_BASE_URL}{path}"
    req_headers = {"Accept": "application/json"}

    if headers:
        req_headers.update(headers)

    if token:
        req_headers["Authorization"] = f"Bearer {token}"

    data = None
    if body is not None:
        if isinstance(body, (dict, list)):
            data = json.dumps(body).encode("utf-8")
            req_headers["Content-Type"] = "application/json"
        elif isinstance(body, bytes):
            data = body
        elif isinstance(body, str):
            data = body.encode("utf-8")

    req = urllib.request.Request(url, data=data, headers=req_headers, method=method)

    try:
        with urllib.request.urlopen(req) as resp:
            content = resp.read().decode("utf-8")
            parsed = None
            if content:
                try:
                    parsed = json.loads(content)
                except Exception:
                    parsed = content
            return resp.status, parsed, dict(resp.headers)
    except urllib.error.HTTPError as e:
        content = e.read().decode("utf-8")
        parsed = None
        if content:
            try:
                parsed = json.loads(content)
            except Exception:
                parsed = content
        return e.code, parsed, dict(e.headers)
    except Exception as e:
        return 0, str(e), {}


def make_raw_socket_request(raw_request: bytes):
    try:
        s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        s.settimeout(5.0)
        s.connect((GATEWAY_HOST, GATEWAY_PORT))
        s.sendall(raw_request)
        response_bytes = b""
        while True:
            chunk = s.recv(4096)
            if not chunk:
                break
            response_bytes += chunk
            if b"\r\n\r\n" in response_bytes:
                break
        s.close()

        lines = response_bytes.decode("utf-8", errors="ignore").split("\r\n")
        status_line = lines[0] if lines else ""
        status_code = int(status_line.split(" ")[1]) if len(status_line.split(" ")) > 1 else 0

        headers = {}
        for line in lines[1:]:
            if not line:
                break
            if ":" in line:
                k, v = line.split(":", 1)
                headers[k.strip()] = v.strip()

        return status_code, None, headers
    except Exception as e:
        return 0, str(e), {}


def test_case(category, method, path, expected_status, description, body=None, token=None, headers=None, pace=0.0, validator=None):
    global total_tests, passed_tests, failed_tests
    total_tests += 1

    status, resp_body, resp_headers = make_request(method, path, body, token, headers, pace)

    success = False
    if isinstance(expected_status, list):
        success = status in expected_status
    else:
        success = (status == expected_status)

    if success and validator:
        try:
            val_res = validator(status, resp_body, resp_headers)
            if val_res is False:
                success = False
        except Exception as e:
            print(f"       {RED}Validator Exception: {e}{RESET}")
            success = False

    if success:
        passed_tests += 1
        print(f"[{category}] {GREEN}✔ PASS{RESET} {method:<6} {path:<45} -> Exp: {expected_status} | Got: {status} - {description}")
    else:
        failed_tests += 1
        print(f"[{category}] {RED}✘ FAIL{RESET} {method:<6} {path:<45} -> Exp: {expected_status} | Got: {status} - {description}")
        if resp_body:
            preview = json.dumps(resp_body) if isinstance(resp_body, dict) else str(resp_body)[:160]
            print(f"       {RED}Response: {preview}{RESET}")

    return status, resp_body, resp_headers


def print_section(title):
    print(f"\n{BOLD}{CYAN}{title}{RESET}")


def run_all_gateway_tests():
    print(f"{BOLD}{BLUE}========================================================================")
    print(f"   ShuffleSeries API Gateway Comprehensive End-to-End Test Suite        ")
    print(f"   Gateway Target: {GATEWAY_BASE_URL}                                  ")
    print(f"========================================================================{RESET}\n")

    admin_token = generate_jwt(roles=["Admin"], permissions=["catalog:admin-media:read", "catalog:platforms:read"])
    premium_token = generate_jwt(roles=["Premium"])
    standard_token = generate_jwt(roles=["Standard"], permissions=["catalog:platforms:read"])
    guest_token = generate_jwt(roles=["Guest"], permissions=["catalog:platforms:read"])

    # -------------------------------------------------------------------------
    # SECTION 1: Sağlık Kontrolleri (Health & Readiness Probes)
    # -------------------------------------------------------------------------
    print_section("1. Health & Readiness Probes (Gateway & Downstream Microservices)")

    test_case("HEALTH", "GET", "/health/live", 200, "Gateway Liveness Probe (200 OK)",
              validator=lambda s, b, h: b.get("status") == "Healthy" and b.get("type") == "Liveness")

    test_case("HEALTH", "GET", "/health/ready", 200, "Gateway Readiness Probe (Redis Connection) (200 OK)",
              validator=lambda s, b, h: b.get("status") == "Healthy" and b.get("type") == "Readiness")

    test_case("HEALTH", "GET", "/catalog-api/health/live", 200, "Downstream Catalog Liveness Probe via Gateway (200 OK)",
              validator=lambda s, b, h: b.get("status") == "Healthy" and b.get("type") == "Liveness")

    test_case("HEALTH", "GET", "/catalog-api/health/ready", 200, "Downstream Catalog Readiness Probe via Gateway (DB, Vault) (200 OK)",
              validator=lambda s, b, h: b.get("status") == "Healthy" and b.get("type") == "Readiness")

    test_case("HEALTH", "GET", "/identity-api/health/live", 200, "Downstream Identity Liveness Probe via Gateway (200 OK)",
              validator=lambda s, b, h: b.get("status") == "Healthy" and b.get("type") == "Liveness")

    test_case("HEALTH", "GET", "/identity-api/health/ready", 200, "Downstream Identity Readiness Probe via Gateway (DB, Redis, RabbitMQ) (200 OK)",
              validator=lambda s, b, h: b.get("status") == "Healthy" and b.get("type") == "Readiness")

    # -------------------------------------------------------------------------
    # SECTION 2: YARP Reverse Proxy Routing & OpenAPI Aggregation
    # -------------------------------------------------------------------------
    print_section("2. YARP Reverse Proxy Routing & OpenAPI Aggregation")

    test_case("ROUTING", "GET", "/catalog-api/openapi/v1.json", 200, "Catalog OpenAPI v3 Specification via Gateway",
              validator=lambda s, b, h: "openapi" in b or "paths" in b)

    test_case("ROUTING", "GET", "/identity-api/openapi/v1.json", 200, "Identity OpenAPI v3 Specification via Gateway",
              validator=lambda s, b, h: "openapi" in b or "paths" in b)

    test_case("ROUTING", "GET", "/api/catalog/platforms", 200, "Catalog Route Forwarding (/api/catalog/platforms)",
              token=standard_token, validator=lambda s, b, h: isinstance(b, list))

    test_case("ROUTING", "POST", "/api/auth/guest", 200, "Identity Route Forwarding (/api/auth/guest)",
              pace=0.25, validator=lambda s, b, h: b.get("isGuest") is True and "accessToken" in b)

    test_case("ROUTING", "GET", "/api/nonexistent/route/xyz", 404, "Unrouted Downstream Route -> 404 Not Found")

    test_case("ROUTING", "GET", "/totally/unknown/path", 404, "Completely Unknown Path -> 404 Not Found")

    # -------------------------------------------------------------------------
    # SECTION 3: Distributed Tracing & Correlation ID Propagation (X-Correlation-ID)
    # -------------------------------------------------------------------------
    print_section("3. Distributed Tracing & Correlation ID (X-Correlation-ID)")

    custom_corr_id = f"e2e-gateway-custom-{uuid.uuid4().hex[:12]}"
    test_case("CORR-ID", "GET", "/health/live", 200, "Custom X-Correlation-ID is Preserved and Echoed in Response Header",
              headers={"X-Correlation-ID": custom_corr_id},
              validator=lambda s, b, h: h.get("X-Correlation-ID") == custom_corr_id or h.get("x-correlation-id") == custom_corr_id)

    test_case("CORR-ID", "GET", "/health/live", 200, "Missing X-Correlation-ID is Auto-Generated as Hex GUID in Response",
              validator=lambda s, b, h: len(h.get("X-Correlation-ID") or h.get("x-correlation-id") or "") >= 32)

    # Header Injection Protection (Control chars rejected/replaced via raw wire test)
    global total_tests, passed_tests, failed_tests
    total_tests += 1
    raw_payload = b"GET /health/live HTTP/1.1\r\nHost: localhost:5001\r\nX-Correlation-ID: bad\x07bell\r\n\r\n"
    s_code, _, s_headers = make_raw_socket_request(raw_payload)
    resp_corr = s_headers.get("X-Correlation-ID") or s_headers.get("x-correlation-id") or ""
    if s_code == 200 and "\x07" not in resp_corr and len(resp_corr) >= 32:
        passed_tests += 1
        print(f"[CORR-ID] {GREEN}✔ PASS{RESET} GET    /health/live                                  -> Exp: 200 | Got: {s_code} - Control Characters in X-Correlation-ID are Sanitized (Security API3)")
    else:
        failed_tests += 1
        print(f"[CORR-ID] {RED}✘ FAIL{RESET} GET    /health/live                                  -> Exp: 200 | Got: {s_code} - Control Characters in X-Correlation-ID Sanitization Failed")

    # Oversized Header Protection (>128 chars rejected/replaced)
    oversized_id = "A" * 150
    test_case("CORR-ID", "GET", "/health/live", 200, "Oversized X-Correlation-ID (>128 chars) is Sanitized to Safe GUID",
              headers={"X-Correlation-ID": oversized_id},
              validator=lambda s, b, h: (h.get("X-Correlation-ID") or h.get("x-correlation-id")) != oversized_id)

    # Correlation ID in ProblemDetails extension on error
    test_case("CORR-ID", "GET", "/api/auth/me", 401, "Correlation ID Present in RFC 7807 ProblemDetails Extensions",
              pace=0.25,
              validator=lambda s, b, h: isinstance(b, dict) and "correlationId" in b)

    # -------------------------------------------------------------------------
    # SECTION 4: Centralized CORS (Cross-Origin Resource Sharing)
    # -------------------------------------------------------------------------
    print_section("4. Centralized CORS Preflight & Headers")

    test_case("CORS", "OPTIONS", "/api/catalog/platforms", [200, 204], "CORS Preflight (OPTIONS) Returns Allowed Methods & Origin",
              headers={
                  "Origin": "http://localhost:3000",
                  "Access-Control-Request-Method": "GET",
                  "Access-Control-Request-Headers": "Authorization,Content-Type,X-Correlation-ID"
              },
              validator=lambda s, b, h: "access-control-allow-origin" in [k.lower() for k in h.keys()])

    test_case("CORS", "GET", "/health/live", 200, "Simple GET Request with Origin Header Includes Access-Control-Allow-Origin",
              headers={"Origin": "http://localhost:3000"},
              validator=lambda s, b, h: any("access-control-allow-origin" in k.lower() for k in h.keys()))

    # -------------------------------------------------------------------------
    # SECTION 5: Gateway Edge Authentication & RBAC Policy Enforcement
    # -------------------------------------------------------------------------
    print_section("5. Gateway Edge Authentication & RBAC Policy Enforcement")
    # /api/shuffle/vip-weekend has StrictLimiter + RequirePremiumRole on Gateway
    # We apply pace=0.25 to stay within the 5 req/s rate limit

    time.sleep(1.2)

    test_case("AUTH-EDGE", "GET", "/api/shuffle/vip-weekend", 401, "VIP Route without Token -> 401 Unauthorized (AUTH_UNAUTHORIZED)",
              pace=0.25,
              validator=lambda s, b, h: isinstance(b, dict) and b.get("code") == "AUTH_UNAUTHORIZED")

    test_case("AUTH-EDGE", "GET", "/api/shuffle/vip-weekend", 401, "VIP Route with Malformed Token -> 401 Unauthorized",
              token="not_a_valid_jwt_token_string", pace=0.25,
              validator=lambda s, b, h: isinstance(b, dict) and b.get("code") == "AUTH_UNAUTHORIZED")

    expired_token = generate_jwt(roles=["Premium"], exp_seconds=-3600)
    test_case("AUTH-EDGE", "GET", "/api/shuffle/vip-weekend", 401, "VIP Route with Expired Token -> 401 Unauthorized",
              token=expired_token, pace=0.25,
              validator=lambda s, b, h: isinstance(b, dict) and b.get("code") == "AUTH_UNAUTHORIZED")

    tampered_token = generate_jwt(roles=["Premium"], secret="WrongSignatureSecretKeyMustFail1234567890!!")
    test_case("AUTH-EDGE", "GET", "/api/shuffle/vip-weekend", 401, "VIP Route with Tampered Signature -> 401 Unauthorized",
              token=tampered_token, pace=0.25,
              validator=lambda s, b, h: isinstance(b, dict) and b.get("code") == "AUTH_UNAUTHORIZED")

    test_case("AUTH-EDGE", "GET", "/api/shuffle/vip-weekend", 403, "VIP Route with Standard User Token -> 403 Forbidden (AUTH_FORBIDDEN)",
              token=standard_token, pace=0.25,
              validator=lambda s, b, h: isinstance(b, dict) and b.get("code") == "AUTH_FORBIDDEN")

    test_case("AUTH-EDGE", "GET", "/api/shuffle/vip-weekend", 403, "VIP Route with Guest Token -> 403 Forbidden (AUTH_FORBIDDEN)",
              token=guest_token, pace=0.25,
              validator=lambda s, b, h: isinstance(b, dict) and b.get("code") == "AUTH_FORBIDDEN")

    # With Premium role, auth policy passes; attempts downstream forward (502 Bad Gateway since shuffle.api is not yet active)
    test_case("AUTH-EDGE", "GET", "/api/shuffle/vip-weekend", [200, 502], "VIP Route with Premium Token -> Auth Passes at Edge",
              token=premium_token, pace=0.25)

    # -------------------------------------------------------------------------
    # SECTION 6: Redis Token & User Session Blacklist Enforcement
    # -------------------------------------------------------------------------
    print_section("6. Redis Token & User Session Blacklist Enforcement at Edge")

    # Cooldown before entering /api/auth/me suite
    time.sleep(1.2)

    # 6.1 Create real session and test clean token passes
    _, guest_res, _ = make_request("POST", "/api/auth/guest", pace=0.25)
    clean_token = guest_res.get("accessToken") if isinstance(guest_res, dict) else None

    test_case("BLACKLIST", "GET", "/api/auth/me", 200, "Valid Unblacklisted Token -> 200 OK",
              token=clean_token, pace=0.25,
              validator=lambda s, b, h: isinstance(b, dict) and b.get("isGuest") is True)

    # 6.2 Token JTI Blacklisted in Redis
    revoked_jti = f"revoked-jti-{uuid.uuid4().hex[:12]}"
    revoked_token = generate_jwt(jti=revoked_jti, roles=["Standard"])
    redis_key = f"shuffleseries:blacklist:token:{revoked_jti}"
    redis_cli_set(redis_key, "revoked", ttl_seconds=60)

    test_case("BLACKLIST", "GET", "/api/auth/me", 401, "Revoked Token (JTI in Redis) -> 401 Unauthorized (AUTH_TOKEN_REVOKED)",
              token=revoked_token, pace=0.25,
              validator=lambda s, b, h: isinstance(b, dict) and b.get("code") == "AUTH_TOKEN_REVOKED" and b.get("title") == "Token Revoked")

    # Clean up Redis key
    redis_cli_del(redis_key)

    # 6.3 User Session Blacklisted in Redis
    user_guid = str(uuid.uuid4())
    user_clean_key = user_guid.replace("-", "")
    user_token = generate_jwt(user_id=user_guid, roles=["Standard"], iat_offset=-10)

    # Set user revocation timestamp to now in Redis
    user_redis_key = f"shuffleseries:blacklist:user:{user_clean_key}"
    redis_cli_set(user_redis_key, str(int(time.time())), ttl_seconds=60)

    test_case("BLACKLIST", "GET", "/api/auth/me", 401, "Revoked User Session (User in Redis) -> 401 Unauthorized (AUTH_TOKEN_REVOKED)",
              token=user_token, pace=0.25,
              validator=lambda s, b, h: isinstance(b, dict) and b.get("code") == "AUTH_TOKEN_REVOKED")

    redis_cli_del(user_redis_key)

    # -------------------------------------------------------------------------
    # SECTION 7: Gateway Rate Limiting & Throttling (StrictLimiter 429)
    # -------------------------------------------------------------------------
    print_section("7. Gateway Rate Limiting & Throttling (StrictLimiter 429)")

    # Cooldown to start with fresh quota
    time.sleep(1.2)

    burst_statuses = []
    print(f"       {CYAN}Firing burst of 8 rapid requests to StrictLimiter-protected endpoint (/api/auth/login)...{RESET}")
    for i in range(8):
        status, _, _ = make_request("POST", "/api/auth/login", body={})
        burst_statuses.append(status)

    first_5_allowed = all(s != 429 for s in burst_statuses[:5])
    sixth_or_more_rejected = any(s == 429 for s in burst_statuses[5:])

    total_tests += 1
    if first_5_allowed and sixth_or_more_rejected:
        passed_tests += 1
        print(f"[RATELIMIT] {GREEN}✔ PASS{RESET} POST   /api/auth/login                               -> Exp: 429 on Burst | Got: {burst_statuses} - StrictLimiter PermitLimit=5 Enforced")
    else:
        failed_tests += 1
        print(f"[RATELIMIT] {RED}✘ FAIL{RESET} POST   /api/auth/login                               -> Exp: 429 on Burst | Got: {burst_statuses} - Rate Limiting Mismatch")

    # Cooldown & verify quota recovery
    print(f"       {CYAN}Waiting 1.2s for Fixed Window Rate Limiter recovery...{RESET}")
    time.sleep(1.2)

    test_case("RATELIMIT", "POST", "/api/auth/login", 400, "StrictLimiter Quota Recovers After Fixed Window (1s)",
              body={})

    # -------------------------------------------------------------------------
    # SUMMARY
    # -------------------------------------------------------------------------
    print(f"\n{BOLD}{BLUE}========================================================================")
    print(f"                     GATEWAY TEST EXECUTION SUMMARY                     ")
    print(f"========================================================================{RESET}")
    print(f"Total Test Cases Executed : {BOLD}{total_tests}{RESET}")
    print(f"Passed                    : {GREEN}{BOLD}{passed_tests}{RESET}")
    print(f"Failed                    : {RED}{BOLD}{failed_tests}{RESET}")
    rate = (passed_tests / total_tests * 100) if total_tests > 0 else 0
    print(f"Success Rate              : {BOLD}{rate:.1f}%{RESET}")
    print(f"{BOLD}{BLUE}========================================================================{RESET}\n")

    if failed_tests == 0:
        print(f"{GREEN}{BOLD}All {passed_tests} API Gateway test cases PASSED successfully!{RESET}\n")
        return 0
    else:
        print(f"{RED}{BOLD}{failed_tests} test cases FAILED! Check logs above.{RESET}\n")
        return 1


if __name__ == "__main__":
    sys.exit(run_all_gateway_tests())
