#!/usr/bin/env python3
"""
ShuffleSeries - Identity Service & API Gateway Comprehensive End-to-End Test Suite
Tests all Identity endpoints through the API Gateway (http://localhost:5001) covering:
- Health Probes (Live & Ready)
- Standard User Registration (Happy Path, Empty, Invalid, Weak, Duplicate 409)
- Authentication / Login (Happy Path, Bad Password 401, Non-Existent 401, Validation 400)
- Current User Profile /me (Bearer token, Missing 401, Malformed 401, Expired 401, Tampered 401)
- Frictionless Anonymous Guest Sessions (Creation, Guest Claims)
- In-Place Guest Conversion to Permanent Account (Happy Path, Non-guest 422, Collision 409, Invalid 400)
- Refresh Token Rotation & Compromise Detection (Rotation, Token Reuse Detection 401, Invalid 401)
- Password Management (Happy Path 204, Old Password Rejection 401, Weak 400, Same Password 400)
- Token Revocation / Logout (Revoke 204, Revoked Token Rejection 401, Not Found 404)
- Social Login (Unsupported Provider 400, Invalid Token 401)
- Account Deletion GDPR / Apple 5.1.1(v) (Soft-Delete 204, Token Blacklisted 401)
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

GATEWAY_BASE_URL = "http://localhost:5001"

# ANSI Colors
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


def b64url(data: bytes) -> str:
    return base64.urlsafe_b64encode(data).rstrip(b"=").decode("utf-8")


def generate_jwt(user_id=None, email=None, roles=None, permissions=None, exp_seconds=3600, nbf_seconds=0, secret=JWT_SECRET, jti=None):
    now = int(time.time())
    header = {"alg": "HS256", "typ": "JWT"}
    payload = {
        "iss": JWT_ISSUER,
        "aud": JWT_AUDIENCE,
        "sub": user_id or str(uuid.uuid4()),
        "jti": jti or str(uuid.uuid4()),
        "email": email or "user@shuffleseries.com",
        "role": roles or ["Standard"],
        "permission": permissions or ["catalog:movies:read"],
        "nbf": now + nbf_seconds,
        "iat": now,
        "exp": now + exp_seconds
    }
    segments = [
        b64url(json.dumps(header).encode("utf-8")),
        b64url(json.dumps(payload).encode("utf-8"))
    ]
    signing_input = ".".join(segments).encode("ascii")
    signature = hmac.new(secret.encode("utf-8"), signing_input, hashlib.sha256).digest()
    return f"{segments[0]}.{segments[1]}.{b64url(signature)}"


def make_request(method: str, path: str, token: str = None, body: dict = None, headers: dict = None):
    # Respect Gateway StrictLimiter (max 5 req/sec on /api/auth)
    time.sleep(0.25)

    url = f"{GATEWAY_BASE_URL}{path}"
    req_headers = {"Accept": "application/json"}
    if headers:
        req_headers.update(headers)
    if token:
        req_headers["Authorization"] = f"Bearer {token}"

    data = None
    if body is not None:
        data = json.dumps(body).encode("utf-8")
        req_headers["Content-Type"] = "application/json"

    req = urllib.request.Request(url, data=data, headers=req_headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=10) as response:
            status = response.status
            resp_body = response.read().decode("utf-8")
            resp_headers = dict(response.headers)
            try:
                parsed_json = json.loads(resp_body) if resp_body else None
            except json.JSONDecodeError:
                parsed_json = resp_body
            return status, parsed_json, resp_headers
    except urllib.error.HTTPError as e:
        status = e.code
        resp_body = e.read().decode("utf-8")
        resp_headers = dict(e.headers)
        try:
            parsed_json = json.loads(resp_body) if resp_body else None
        except json.JSONDecodeError:
            parsed_json = resp_body
        return status, parsed_json, resp_headers
    except Exception as e:
        return 0, str(e), {}


def test_case(category: str, name: str, method: str, path: str, expected_status: int,
              token: str = None, body: dict = None, headers: dict = None, validate_fn=None):
    global total_tests, passed_tests, failed_tests
    total_tests += 1

    status, response_body, resp_headers = make_request(method, path, token=token, body=body, headers=headers)
    status_match = (status == expected_status)
    custom_valid = True
    custom_err = ""

    if status_match and validate_fn:
        try:
            valid, msg = validate_fn(response_body, resp_headers)
            if not valid:
                custom_valid = False
                custom_err = msg
        except Exception as ex:
            custom_valid = False
            custom_err = f"Validation exception: {ex}"

    passed = status_match and custom_valid

    if passed:
        passed_tests += 1
        print(f"[{category}] {GREEN}✔ PASS{RESET} {method:<6} {path:<45} -> Exp: {expected_status} | Got: {status} - {name}")
    else:
        failed_tests += 1
        print(f"[{category}] {RED}✘ FAIL{RESET} {method:<6} {path:<45} -> Exp: {expected_status} | Got: {status} - {name}")
        if not status_match:
            print(f"    {YELLOW}Details:{RESET} {json.dumps(response_body, indent=2, ensure_ascii=False) if isinstance(response_body, (dict, list)) else response_body}")
        if not custom_valid:
            print(f"    {YELLOW}Custom validation failed:{RESET} {custom_err}")

    return response_body, resp_headers


def run_all_tests():
    print(f"\n{BOLD}{CYAN}========================================================================{RESET}")
    print(f"{BOLD}{CYAN}   ShuffleSeries Identity & API Gateway Comprehensive Test Suite        {RESET}")
    print(f"{BOLD}{CYAN}   Gateway Target: {GATEWAY_BASE_URL}                                  {RESET}")
    print(f"{BOLD}{CYAN}========================================================================{RESET}\n")

    # -------------------------------------------------------------
    # 1. Health Probes
    # -------------------------------------------------------------
    print(f"\n{BOLD}1. Health Probes (Edge & Internal){RESET}")
    test_case("HEALTH", "Gateway Liveness Probe", "GET", "/health/live", 200)
    test_case("HEALTH", "Gateway Readiness Probe", "GET", "/health/ready", 200)
    test_case("HEALTH", "Identity Liveness Probe via Gateway", "GET", "/identity-api/health/live", 200)
    test_case("HEALTH", "Identity Readiness Probe via Gateway (DB, Redis, Bus)", "GET", "/identity-api/health/ready", 200)

    # -------------------------------------------------------------
    # 2. Standard User Registration
    # -------------------------------------------------------------
    print(f"\n{BOLD}2. Standard User Registration (/api/auth/register){RESET}")
    unique_suffix = f"{int(time.time() * 1000) % 1000000}"
    reg_email = f"user_{unique_suffix}@shuffleseries.com"
    reg_password = "Password123!"

    reg_resp, _ = test_case("REGISTER", "Successful User Registration (200 OK)", "POST", "/api/auth/register", 200,
                            body={"email": reg_email, "password": reg_password},
                            validate_fn=lambda r, h: (
                                bool(r and r.get("accessToken") and r.get("refreshToken") and "Standard" in r.get("roles", [])),
                                "Missing tokens or Standard role"
                            ))

    user_id = reg_resp.get("userId") if isinstance(reg_resp, dict) else None
    access_token = reg_resp.get("accessToken") if isinstance(reg_resp, dict) else None
    refresh_token = reg_resp.get("refreshToken") if isinstance(reg_resp, dict) else None

    # Duplicate Email Conflict -> 409 Conflict
    test_case("REGISTER", "Duplicate Email -> 409 Conflict", "POST", "/api/auth/register", 409,
              body={"email": reg_email, "password": reg_password})

    # Empty Email -> 400 Bad Request
    test_case("REGISTER", "Empty Email -> 400 Bad Request", "POST", "/api/auth/register", 400,
              body={"email": "", "password": reg_password})

    # Invalid Email Format -> 400 Bad Request
    test_case("REGISTER", "Invalid Email Format -> 400 Bad Request", "POST", "/api/auth/register", 400,
              body={"email": "invalid-email-format", "password": reg_password})

    # Weak Password -> 400 Bad Request
    test_case("REGISTER", "Weak Password (<6 chars) -> 400 Bad Request", "POST", "/api/auth/register", 400,
              body={"email": f"weak_{unique_suffix}@shuffleseries.com", "password": "123"})

    # -------------------------------------------------------------
    # 3. User Authentication & Login
    # -------------------------------------------------------------
    print(f"\n{BOLD}3. User Authentication & Login (/api/auth/login){RESET}")
    login_resp, _ = test_case("LOGIN", "Successful Login (200 OK)", "POST", "/api/auth/login", 200,
                              body={"email": reg_email, "password": reg_password},
                              validate_fn=lambda r, h: (
                                  bool(r and r.get("accessToken") and r.get("refreshToken")),
                                  "Tokens missing in login response"
                              ))

    # Incorrect Password -> 401 Unauthorized
    test_case("LOGIN", "Incorrect Password -> 401 Unauthorized", "POST", "/api/auth/login", 401,
              body={"email": reg_email, "password": "WrongPassword999!"})

    # Non-Existent User -> 401 Unauthorized
    test_case("LOGIN", "Non-Existent User -> 401 Unauthorized", "POST", "/api/auth/login", 401,
              body={"email": "nonexistent_ghost_user@shuffleseries.com", "password": reg_password})

    # Empty Password -> 400 Bad Request
    test_case("LOGIN", "Empty Password -> 400 Bad Request", "POST", "/api/auth/login", 400,
              body={"email": reg_email, "password": ""})

    # -------------------------------------------------------------
    # 4. Profile & Current User Inspection (/api/auth/me)
    # -------------------------------------------------------------
    print(f"\n{BOLD}4. Profile & Effective Permissions (/api/auth/me){RESET}")
    if access_token:
        test_case("PROFILE", "Get Current User Profile (200 OK)", "GET", "/api/auth/me", 200,
                  token=access_token,
                  validate_fn=lambda r, h: (
                      r.get("email") == reg_email and r.get("isGuest") is False,
                      f"Email mismatch or isGuest not False: {r}"
                  ))

    # Missing Token -> 401 Unauthorized
    test_case("PROFILE", "Missing Token -> 401 Unauthorized", "GET", "/api/auth/me", 401, token=None)

    # Malformed Token -> 401 Unauthorized
    test_case("PROFILE", "Malformed Token -> 401 Unauthorized", "GET", "/api/auth/me", 401, token="not.a.valid.jwt")

    # Expired Token -> 401 Unauthorized
    expired_jwt = generate_jwt(exp_seconds=-60)
    test_case("PROFILE", "Expired Token -> 401 Unauthorized", "GET", "/api/auth/me", 401, token=expired_jwt)

    # Tampered Signature -> 401 Unauthorized
    tampered_jwt = access_token[:-10] + "ABCDEFGHIJ" if access_token else "tampered"
    test_case("PROFILE", "Tampered Signature Token -> 401 Unauthorized", "GET", "/api/auth/me", 401, token=tampered_jwt)

    # -------------------------------------------------------------
    # 5. Anonymous Guest Sessions
    # -------------------------------------------------------------
    print(f"\n{BOLD}5. Anonymous Frictionless Guest Session (/api/auth/guest){RESET}")
    guest_resp, _ = test_case("GUEST", "Create Anonymous Guest Session (200 OK)", "POST", "/api/auth/guest", 200,
                              validate_fn=lambda r, h: (
                                  r.get("isGuest") is True and "Guest" in r.get("roles", []) and bool(r.get("accessToken")),
                                  f"Invalid guest response: {r}"
                              ))

    guest_user_id = guest_resp.get("userId") if isinstance(guest_resp, dict) else None
    guest_access_token = guest_resp.get("accessToken") if isinstance(guest_resp, dict) else None

    if guest_access_token:
        test_case("GUEST", "Get Profile with Guest Token (200 OK - isGuest=true)", "GET", "/api/auth/me", 200,
                  token=guest_access_token,
                  validate_fn=lambda r, h: (
                      r.get("isGuest") is True and "Guest" in r.get("roles", []),
                      f"Expected isGuest=True, got: {r}"
                  ))

    # -------------------------------------------------------------
    # 6. Convert Guest to Permanent Account
    # -------------------------------------------------------------
    print(f"\n{BOLD}6. Convert Guest to Permanent Account (/api/auth/convert-guest){RESET}")
    conv_email = f"converted_{unique_suffix}@shuffleseries.com"
    conv_pass = "ConvertedPass123!"

    if guest_access_token:
        # Successful in-place elevation
        test_case("CONVERT", "Convert Guest to Permanent Account (200 OK)", "POST", "/api/auth/convert-guest", 200,
                  token=guest_access_token,
                  body={"email": conv_email, "password": conv_pass},
                  validate_fn=lambda r, h: (
                      r.get("userId") == guest_user_id and r.get("isGuest") is False and r.get("email") == conv_email,
                      f"Conversion failed or UserId changed: {r}"
                  ))

        # Attempt to convert already registered user -> 422 Unprocessable Entity
        test_case("CONVERT", "Convert Already Registered User -> 422 Unprocessable Entity", "POST", "/api/auth/convert-guest", 422,
                  token=guest_access_token,
                  body={"email": f"another_{unique_suffix}@shuffleseries.com", "password": conv_pass})

    # Collision with existing email on convert
    guest2_resp, _ = test_case("CONVERT", "Create Second Guest for Collision Test", "POST", "/api/auth/guest", 200)
    guest2_token = guest2_resp.get("accessToken") if isinstance(guest2_resp, dict) else None
    if guest2_token:
        test_case("CONVERT", "Convert with Existing Email -> 409 Conflict", "POST", "/api/auth/convert-guest", 409,
                  token=guest2_token,
                  body={"email": reg_email, "password": "Password123!"})

    # Missing token on convert -> 401 Unauthorized
    test_case("CONVERT", "Convert without Auth Token -> 401 Unauthorized", "POST", "/api/auth/convert-guest", 401,
              token=None,
              body={"email": f"no_token_{unique_suffix}@shuffleseries.com", "password": "Password123!"})

    # -------------------------------------------------------------
    # 7. Refresh Token Rotation & Security (OWASP API4)
    # -------------------------------------------------------------
    print(f"\n{BOLD}7. Refresh Token Rotation & Compromise Detection (/api/auth/refresh){RESET}")
    # Login again to get a fresh refresh token
    fresh_login, _ = test_case("REFRESH", "Login to get Fresh Refresh Token", "POST", "/api/auth/login", 200,
                               body={"email": reg_email, "password": reg_password})
    active_refresh_token = fresh_login.get("refreshToken") if isinstance(fresh_login, dict) else None

    if active_refresh_token:
        # Happy Path Rotation
        rot_resp, _ = test_case("REFRESH", "Rotate Refresh Token (200 OK)", "POST", "/api/auth/refresh", 200,
                                body={"refreshToken": active_refresh_token},
                                validate_fn=lambda r, h: (
                                    bool(r.get("accessToken") and r.get("refreshToken") != active_refresh_token),
                                    "Rotated token is identical or missing"
                                ))

        new_refresh_token = rot_resp.get("refreshToken") if isinstance(rot_resp, dict) else None

        # Re-use of old refresh token -> Compromise detection (401 Unauthorized)
        test_case("REFRESH", "Replay Old Refresh Token -> 401 Unauthorized (Reuse Detected)", "POST", "/api/auth/refresh", 401,
                  body={"refreshToken": active_refresh_token})

    # Invalid / Fake refresh token -> 401 Unauthorized
    test_case("REFRESH", "Invalid Refresh Token -> 401 Unauthorized", "POST", "/api/auth/refresh", 401,
              body={"refreshToken": "completely_fake_refresh_token_123456789"})

    # Empty refresh token -> 400 Bad Request
    test_case("REFRESH", "Empty Refresh Token -> 400 Bad Request", "POST", "/api/auth/refresh", 400,
              body={"refreshToken": ""})

    # -------------------------------------------------------------
    # 8. Password Change & Session Invalidation
    # -------------------------------------------------------------
    print(f"\n{BOLD}8. Password Change & Session Invalidation (/api/auth/change-password){RESET}")
    pwd_email = f"user_pwd_{unique_suffix}@shuffleseries.com"
    pwd_orig_pass = "OriginalPass123!"
    pwd_reg, _ = test_case("PASSWORD", "Register User for Password Change Test", "POST", "/api/auth/register", 200,
                           body={"email": pwd_email, "password": pwd_orig_pass})
    pwd_token = pwd_reg.get("accessToken") if isinstance(pwd_reg, dict) else None
    new_reg_password = "BrandNewPassword2026!"

    if pwd_token:
        # Wrong current password -> 401 Unauthorized
        test_case("PASSWORD", "Wrong Current Password -> 401 Unauthorized", "POST", "/api/auth/change-password", 401,
                  token=pwd_token,
                  body={"currentPassword": "IncorrectPassword999!", "newPassword": new_reg_password})

        # Weak new password -> 400 Bad Request
        test_case("PASSWORD", "Weak New Password (<6 chars) -> 400 Bad Request", "POST", "/api/auth/change-password", 400,
                  token=pwd_token,
                  body={"currentPassword": pwd_orig_pass, "newPassword": "123"})

        # New password same as current -> 400 Bad Request
        test_case("PASSWORD", "New Password Same As Current -> 400 Bad Request", "POST", "/api/auth/change-password", 400,
                  token=pwd_token,
                  body={"currentPassword": pwd_orig_pass, "newPassword": pwd_orig_pass})

        # Happy Path Password Change -> 204 NoContent
        test_case("PASSWORD", "Successful Password Change (204 NoContent)", "POST", "/api/auth/change-password", 204,
                  token=pwd_token,
                  body={"currentPassword": pwd_orig_pass, "newPassword": new_reg_password})

        # Verify old password no longer works
        test_case("PASSWORD", "Login with Old Password -> 401 Unauthorized", "POST", "/api/auth/login", 401,
                  body={"email": pwd_email, "password": pwd_orig_pass})

        # Verify new password works
        test_case("PASSWORD", "Login with New Password (200 OK)", "POST", "/api/auth/login", 200,
                  body={"email": pwd_email, "password": new_reg_password})

    # Change password without auth token -> 401 Unauthorized
    test_case("PASSWORD", "Change Password without Auth Token -> 401 Unauthorized", "POST", "/api/auth/change-password", 401,
              token=None,
              body={"currentPassword": pwd_orig_pass, "newPassword": new_reg_password})

    # -------------------------------------------------------------
    # 9. Token Revocation / Logout (/api/auth/revoke)
    # -------------------------------------------------------------
    print(f"\n{BOLD}9. Token Revocation & Logout (/api/auth/revoke){RESET}")
    rev_login, _ = test_case("REVOKE", "Login to get Tokens to Revoke", "POST", "/api/auth/login", 200,
                             body={"email": pwd_email, "password": new_reg_password})
    rev_access = rev_login.get("accessToken") if isinstance(rev_login, dict) else None
    rev_refresh = rev_login.get("refreshToken") if isinstance(rev_login, dict) else None

    if rev_refresh:
        # Happy Path Revocation -> 204 NoContent
        test_case("REVOKE", "Revoke Active Token Pair (204 NoContent)", "POST", "/api/auth/revoke", 204,
                  token=rev_access,
                  body={"refreshToken": rev_refresh, "accessToken": rev_access})

        # Attempt to use revoked refresh token -> 401 Unauthorized
        test_case("REVOKE", "Refresh with Revoked Token -> 401 Unauthorized", "POST", "/api/auth/refresh", 401,
                  body={"refreshToken": rev_refresh})

    # Non-existent refresh token -> 404 NotFound
    test_case("REVOKE", "Revoke Non-Existent Token -> 404 NotFound", "POST", "/api/auth/revoke", 404,
              body={"refreshToken": "non_existent_refresh_token_to_revoke_12345"})

    # Empty refresh token -> 400 Bad Request
    test_case("REVOKE", "Revoke with Empty Token -> 400 Bad Request", "POST", "/api/auth/revoke", 400,
              body={"refreshToken": ""})

    # -------------------------------------------------------------
    # 10. Social Login (/api/auth/social-login)
    # -------------------------------------------------------------
    print(f"\n{BOLD}10. Social Login Edge Cases (/api/auth/social-login){RESET}")
    # Unsupported provider -> 400 Bad Request
    test_case("SOCIAL", "Unsupported Social Provider (Facebook) -> 400 Bad Request", "POST", "/api/auth/social-login", 400,
              body={"provider": "Facebook", "idToken": "fake_fb_token"})

    # Invalid external token -> 401 Unauthorized
    test_case("SOCIAL", "Invalid External Token (Google) -> 401 Unauthorized", "POST", "/api/auth/social-login", 401,
              body={"provider": "Google", "idToken": "invalid_unverifiable_mock_token"})

    # -------------------------------------------------------------
    # 11. Account Deletion (GDPR / Apple Guideline 5.1.1(v))
    # -------------------------------------------------------------
    print(f"\n{BOLD}11. Account Deletion (/api/auth/account){RESET}")
    # Create an isolated user to delete
    del_email = f"delete_me_{unique_suffix}@shuffleseries.com"
    del_reg, _ = test_case("DELETE", "Create User for Deletion Test", "POST", "/api/auth/register", 200,
                           body={"email": del_email, "password": "Password123!"})
    del_token = del_reg.get("accessToken") if isinstance(del_reg, dict) else None

    # Delete without token -> 401 Unauthorized
    test_case("DELETE", "Delete Account without Token -> 401 Unauthorized", "DELETE", "/api/auth/account", 401, token=None)

    if del_token:
        # Happy Path Soft-Delete -> 204 NoContent
        test_case("DELETE", "Delete Account with Bearer Token (204 NoContent)", "DELETE", "/api/auth/account", 204,
                  token=del_token)

        # Profile fetch after deletion -> 401 Unauthorized (User soft deleted / token blacklisted)
        test_case("DELETE", "Profile Fetch after Deletion -> 401 Unauthorized", "GET", "/api/auth/me", 401,
                  token=del_token)

    # -------------------------------------------------------------
    # Summary
    # -------------------------------------------------------------
    print(f"\n{BOLD}{CYAN}========================================================================{RESET}")
    print(f"{BOLD}{CYAN}                     IDENTITY TEST EXECUTION SUMMARY                    {RESET}")
    print(f"{BOLD}{CYAN}========================================================================{RESET}")
    print(f"Total Test Cases Executed : {total_tests}")
    print(f"Passed                    : {GREEN}{passed_tests}{RESET}")
    print(f"Failed                    : {RED}{failed_tests}{RESET}")
    pct = (passed_tests / total_tests * 100) if total_tests > 0 else 0
    print(f"Success Rate              : {GREEN if pct == 100 else YELLOW}{pct:.1f}%{RESET}")
    print(f"{BOLD}{CYAN}========================================================================{RESET}\n")

    if failed_tests > 0:
        print(f"{RED}{BOLD}Some test cases failed! Review details above.{RESET}\n")
        sys.exit(1)
    else:
        print(f"{GREEN}{BOLD}All {total_tests} Identity test cases PASSED successfully through API Gateway!{RESET}\n")


if __name__ == "__main__":
    run_all_tests()
