#!/usr/bin/env python3
"""
ShuffleSeries Shuffle Engine & API Gateway End-to-End Test Suite (Task 4.1)
Tests Shuffle Engine infrastructure, Cold-Start Cache Warming via Catalog gRPC,
RBAC/Security boundaries, and Live Event-Driven Choreography (Catalog Outbox -> RabbitMQ -> ShuffleEngine Inbox -> Redis)
through the YARP API Gateway (http://localhost:5001).
"""

import os
import sys
import json
import time
import uuid
import hmac
import hashlib
import base64
import urllib.request
import urllib.error

GATEWAY_BASE_URL = os.environ.get("GATEWAY_BASE_URL", "http://localhost:5001")
JWT_SECRET = os.environ.get("JWT_SECRET", "SuperSecretSecretKeyForJwtAuthenticationTokens2026!!")
JWT_ISSUER = "ShuffleSeries.Identity"
JWT_AUDIENCE = "ShuffleSeries.Microservices"

GREEN = "\033[92m"
RED = "\033[91m"
CYAN = "\033[96m"
BOLD = "\033[1m"
RESET = "\033[0m"


def b64url(data: bytes) -> str:
    return base64.urlsafe_b64encode(data).rstrip(b"=").decode("utf-8")


def create_jwt(payload_extra: dict, secret: str = JWT_SECRET, expired: bool = False) -> str:
    now = int(time.time())
    header = {"alg": "HS256", "typ": "JWT"}
    payload = {
        "sub": str(uuid.uuid4()),
        "email": "shuffle-e2e@shuffleseries.com",
        "jti": str(uuid.uuid4()),
        "iat": now - 7200 if expired else now,
        "nbf": now - 7200 if expired else now,
        "exp": now - 3600 if expired else now + 3600,
        "iss": JWT_ISSUER,
        "aud": JWT_AUDIENCE,
        **payload_extra,
    }
    h_b64 = b64url(json.dumps(header).encode("utf-8"))
    p_b64 = b64url(json.dumps(payload, separators=(",", ":")).encode("utf-8"))
    msg = f"{h_b64}.{p_b64}".encode("utf-8")
    sig = b64url(hmac.new(secret.encode("utf-8"), msg, hashlib.sha256).digest())
    return f"{h_b64}.{p_b64}.{sig}"


def create_none_alg_jwt(payload_extra: dict) -> str:
    now = int(time.time())
    header = {"alg": "none", "typ": "JWT"}
    payload = {
        "sub": str(uuid.uuid4()),
        "email": "attacker@evil.com",
        "jti": str(uuid.uuid4()),
        "iat": now,
        "nbf": now,
        "exp": now + 3600,
        "iss": JWT_ISSUER,
        "aud": JWT_AUDIENCE,
        **payload_extra,
    }
    h_b64 = b64url(json.dumps(header).encode("utf-8"))
    p_b64 = b64url(json.dumps(payload, separators=(",", ":")).encode("utf-8"))
    return f"{h_b64}.{p_b64}."


ALL_PERMISSIONS = [
    "catalog:movies:read",
    "catalog:movies:create",
    "catalog:movies:update",
    "catalog:movies:delete",
    "shuffle:basic",
    "shuffle:vip",
]

ADMIN_TOKEN = create_jwt({"role": ["Admin"], "permission": ALL_PERMISSIONS})
STANDARD_TOKEN = create_jwt({"role": ["Standard"], "permission": ["shuffle:basic"]})
GUEST_TOKEN = create_jwt({"role": ["Guest"], "permission": ["shuffle:basic"], "is_guest": "true"})
EXPIRED_TOKEN = create_jwt({"role": ["Admin"], "permission": ALL_PERMISSIONS}, expired=True)
TAMPERED_TOKEN = create_jwt(
    {"role": ["Admin"], "permission": ALL_PERMISSIONS},
    secret="InvalidSecretKeyUsedForTamperVerificationTest!!",
)
NONE_ALG_TOKEN = create_none_alg_jwt({"role": ["Admin"], "permission": ALL_PERMISSIONS})

test_results = []


def request(method: str, path: str, token: str = None, body: dict = None):
    time.sleep(0.02)
    url = f"{GATEWAY_BASE_URL}{path}"
    data = json.dumps(body).encode("utf-8") if body is not None else None
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Accept", "application/json, application/problem+json")
    if body is not None:
        req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", f"Bearer {token}")

    try:
        with urllib.request.urlopen(req, timeout=15) as resp:
            content = resp.read().decode("utf-8")
            parsed = json.loads(content) if content else None
            return resp.status, parsed, dict(resp.headers)
    except urllib.error.HTTPError as err:
        content = err.read().decode("utf-8")
        try:
            parsed = json.loads(content)
        except Exception:
            parsed = content
        return err.code, parsed, dict(err.headers)
    except Exception as ex:
        return 0, str(ex), {}


def record_result(category: str, name: str, passed: bool, detail: str = ""):
    icon = f"{GREEN}✔{RESET}" if passed else f"{RED}✘{RESET}"
    suffix = f" ({detail})" if detail else ""
    print(f"  {icon} [{category}] {name}{suffix}")
    test_results.append({"category": category, "name": name, "passed": passed})


def test_case(category: str, name: str, method: str, path: str, expected_status: int, token: str = None, body: dict = None, validate_fn=None):
    status, resp_body, headers = request(method, path, token=token, body=body)
    passed = status == expected_status
    detail = f"HTTP {status}"
    if passed and validate_fn is not None:
        ok, msg = validate_fn(resp_body, headers)
        passed = ok
        if msg:
            detail = f"{detail} | {msg}"
    elif not passed:
        detail = f"Expected {expected_status}, got {status}: {resp_body}"

    record_result(category, name, passed, detail)
    return resp_body, headers


def run_all_tests() -> int:
    print(f"\n{BOLD}{CYAN}=== Shuffle Engine & Event Choreography E2E Suite ==={RESET}")

    # 1. Health & OpenAPI Aggregation
    print(f"\n{BOLD}1. Gateway Health Routing & OpenAPI Aggregation{RESET}")
    test_case("HEALTH", "Shuffle Engine Liveness via Gateway (200 OK)", "GET", "/shuffle-api/health/live", 200)
    test_case(
        "OPENAPI",
        "Shuffle Engine OpenAPI Schema via Gateway (200 OK)",
        "GET",
        "/shuffle-api/openapi/v1.json",
        200,
        validate_fn=lambda b, _: (
            isinstance(b, dict) and "/api/shuffle/admin/pool-status" in json.dumps(b),
            "OpenAPI paths verified",
        ),
    )

    # 2. Authentication & RBAC Security Boundaries (OWASP API1/API2/API5)
    print(f"\n{BOLD}2. Edge & Endpoint Security Boundaries (401 / 403 RFC 7807){RESET}")
    test_case("SECURITY", "Missing Token on Pool Status -> 401", "GET", "/api/shuffle/admin/pool-status", 401)
    test_case("SECURITY", "Expired Token on Pool Status -> 401", "GET", "/api/shuffle/admin/pool-status", 401, token=EXPIRED_TOKEN)
    test_case("SECURITY", "Tampered Signature Token -> 401", "GET", "/api/shuffle/admin/pool-status", 401, token=TAMPERED_TOKEN)
    test_case("SECURITY", "Alg=none Forged Token -> 401", "GET", "/api/shuffle/admin/pool-status", 401, token=NONE_ALG_TOKEN)
    test_case("SECURITY", "Guest User on Admin Pool Status -> 403", "GET", "/api/shuffle/admin/pool-status", 403, token=GUEST_TOKEN)
    test_case("SECURITY", "Standard User on Admin Pool Status -> 403", "GET", "/api/shuffle/admin/pool-status", 403, token=STANDARD_TOKEN)
    test_case(
        "SECURITY",
        "Standard User on Admin Warm Cache -> 403",
        "POST",
        "/api/shuffle/admin/warm-cache",
        403,
        token=STANDARD_TOKEN,
        body={"forceRebuild": True},
    )

    # 3. Input Validation (FluentValidation)
    print(f"\n{BOLD}3. Input Validation & Control-Character Defense (400){RESET}")
    test_case(
        "VALIDATION",
        "Oversized TriggerReason (>100 chars) -> 400",
        "POST",
        "/api/shuffle/admin/warm-cache",
        400,
        token=ADMIN_TOKEN,
        body={"forceRebuild": False, "triggerReason": "A" * 105},
    )
    test_case(
        "VALIDATION",
        "NUL-Byte in TriggerReason -> 400",
        "POST",
        "/api/shuffle/admin/warm-cache",
        400,
        token=ADMIN_TOKEN,
        body={"forceRebuild": False, "triggerReason": "bad\u0000input"},
    )

    # 4. Cold-Start / Forced Cache Warming via Catalog gRPC (HTTP/2 h2c)
    print(f"\n{BOLD}4. Catalog gRPC Bulk Cache Warming & Safety Net Verification{RESET}")
    test_case(
        "WARMING",
        "Force Full Cache Warming via Catalog gRPC (200 OK)",
        "POST",
        "/api/shuffle/admin/warm-cache",
        200,
        token=ADMIN_TOKEN,
        body={"forceRebuild": True, "triggerReason": "E2E full sync verification"},
        validate_fn=lambda b, _: (
            isinstance(b, dict)
            and b.get("wasSkipped") is False
            and b.get("moviesSynced", 0) >= 1
            and b.get("seriesSynced", 0) >= 1
            and b.get("standaloneEpisodesSynced", 0) >= 4
            and b.get("safetyNetItemsCount", 0) >= 6,
            f"movies={b.get('moviesSynced')}, series={b.get('seriesSynced')}, episodes={b.get('standaloneEpisodesSynced')}, safetyNet={b.get('safetyNetItemsCount')}",
        ),
    )

    test_case(
        "WARMING",
        "Subsequent Non-Forced Warm Skips Idempotently (200 OK)",
        "POST",
        "/api/shuffle/admin/warm-cache",
        200,
        token=ADMIN_TOKEN,
        body={"forceRebuild": False, "triggerReason": "Idempotent sentinel check"},
        validate_fn=lambda b, _: (
            isinstance(b, dict) and b.get("wasSkipped") is True,
            f"wasSkipped={b.get('wasSkipped')}",
        ),
    )

    baseline_status, _ = test_case(
        "POOL",
        "Get Live Pool Status (200 OK)",
        "GET",
        "/api/shuffle/admin/pool-status",
        200,
        token=ADMIN_TOKEN,
        validate_fn=lambda b, _: (
            isinstance(b, dict)
            and b.get("isWarmed") is True
            and b.get("moviesCount", 0) >= 1
            and b.get("seriesCount", 0) >= 1
            and b.get("standaloneEpisodesCount", 0) >= 4,
            f"isWarmed={b.get('isWarmed')}, movies={b.get('moviesCount')}, episodes={b.get('standaloneEpisodesCount')}",
        ),
    )

    # 5. Live Event-Driven Choreography: Catalog Outbox -> RabbitMQ -> ShuffleEngine Inbox -> Redis
    print(f"\n{BOLD}5. Live Event Choreography (Catalog -> RabbitMQ -> ShuffleEngine Inbox -> Redis){RESET}")
    if isinstance(baseline_status, dict):
        base_movies = baseline_status.get("moviesCount", 0)
        base_sn_movies = baseline_status.get("safetyNetMoviesCount", 0)

        rand_seed = int(time.time() * 1000) % 8000000 + 1000000
        create_movie_body = {
            "tmdbId": rand_seed,
            "imdbId": f"tt{rand_seed:07d}",
            "releaseYear": 2024,
            "runtimeMinutes": 132,
            "posterUrl": "https://image.tmdb.org/t/p/original/dune2.jpg",
            "backdropUrl": "https://image.tmdb.org/t/p/original/dune2-bg.jpg",
            "languageCode": "en",
            "title": f"Dune E2E Sync {rand_seed}",
            "overview": "Live event choreography verification movie.",
            "tagline": "Long live the fighters.",
            "director": "Denis Villeneuve",
            "imdbRating": 8.5,
            "imdbVotes": 550000,
            "platformIds": [],
            "moodIds": [],
            "genreIds": [],
        }

        created_resp, _ = test_case(
            "CHOREOGRAPHY",
            "Create High-Rated Movie in Catalog (201 Created)",
            "POST",
            "/api/catalog/movies",
            201,
            token=ADMIN_TOKEN,
            body=create_movie_body,
        )
        movie_id = created_resp if isinstance(created_resp, str) else None

        if movie_id:
            synced_create = False
            deadline = time.time() + 18.0
            latest_movies = base_movies
            latest_sn = base_sn_movies
            while time.time() < deadline:
                st, body, _ = request("GET", "/api/shuffle/admin/pool-status", token=ADMIN_TOKEN)
                if st == 200 and isinstance(body, dict):
                    latest_movies = body.get("moviesCount", 0)
                    latest_sn = body.get("safetyNetMoviesCount", 0)
                    if latest_movies >= base_movies + 1 and latest_sn >= base_sn_movies + 1:
                        synced_create = True
                        break
                time.sleep(0.75)

            record_result(
                "CHOREOGRAPHY",
                "MediaCreatedEvent Synced to ShuffleEngine Redis Pool & SafetyNet",
                synced_create,
                f"movies {base_movies}->{latest_movies}, safetyNet {base_sn_movies}->{latest_sn}",
            )

            # Delete Movie and verify MediaDeletedEvent cleans Redis Pool & SafetyNet
            test_case(
                "CHOREOGRAPHY",
                "Delete Movie in Catalog (204 NoContent)",
                "DELETE",
                f"/api/catalog/movies/{movie_id}",
                204,
                token=ADMIN_TOKEN,
            )

            synced_delete = False
            deadline = time.time() + 18.0
            while time.time() < deadline:
                st, body, _ = request("GET", "/api/shuffle/admin/pool-status", token=ADMIN_TOKEN)
                if st == 200 and isinstance(body, dict):
                    latest_movies = body.get("moviesCount", 0)
                    latest_sn = body.get("safetyNetMoviesCount", 0)
                    if latest_movies == base_movies and latest_sn == base_sn_movies:
                        synced_delete = True
                        break
                time.sleep(0.75)

            record_result(
                "CHOREOGRAPHY",
                "MediaDeletedEvent Atomically Removed Movie from ShuffleEngine Redis",
                synced_delete,
                f"movies->{latest_movies}, safetyNet->{latest_sn}",
            )

    passed_count = sum(1 for r in test_results if r["passed"])
    total_count = len(test_results)
    print(f"\n{BOLD}Shuffle Engine E2E Summary: {passed_count}/{total_count} passed.{RESET}")
    return 0 if passed_count == total_count else 1


if __name__ == "__main__":
    sys.exit(run_all_tests())
