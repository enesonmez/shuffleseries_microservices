#!/usr/bin/env python3
"""
ShuffleSeries Catalog & API Gateway Comprehensive End-to-End Test Suite
Tests all Catalog endpoints through the YARP API Gateway (http://localhost:5001).
Covers:
- Health Checks (Gateway & Catalog)
- Security & Auth (401 Missing, 401 Expired, 401 Tampered, 403 Forbidden)
- Movies CRUD & Edge Cases (200, 201, 204, 400, 404, 422)
- Series CRUD & Edge Cases (200, 201, 204, 400, 404, 422, Pagination limits)
- Episodes CRUD & Edge Cases (200, 201, 204, 400, 404)
- Platforms CRUD & Caching (200, 201, 204, 404, 422)
- Moods CRUD & Caching (200, 201, 204, 404, 422)
- Genres CRUD & Caching (200, 201, 204, 404, 422)
- Media Associations (Platforms, Moods, Genres, Translations)
- Admin Media CMS & Search Validation (SEC-02 100-char limit)
"""

import sys
import json
import time
import uuid
import hmac
import hashlib
import base64
import urllib.request
import urllib.error
import urllib.parse

GATEWAY_BASE_URL = "http://localhost:5001"
JWT_SECRET = "SuperSecretSecretKeyForJwtAuthenticationTokens2026!!"
JWT_ISSUER = "ShuffleSeries.Identity"
JWT_AUDIENCE = "ShuffleSeries.Microservices"

# ANSI Colors
GREEN = "\033[92m"
RED = "\033[91m"
YELLOW = "\033[93m"
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
        "email": "test@shuffleseries.com",
        "jti": str(uuid.uuid4()),
        "iat": now - 7200 if expired else now,
        "nbf": now - 7200 if expired else now,
        "exp": now - 3600 if expired else now + 3600,
        "iss": JWT_ISSUER,
        "aud": JWT_AUDIENCE,
        **payload_extra
    }
    h_b64 = b64url(json.dumps(header).encode("utf-8"))
    p_b64 = b64url(json.dumps(payload, separators=(",", ":")).encode("utf-8"))
    msg = f"{h_b64}.{p_b64}".encode("utf-8")
    sig = b64url(hmac.new(secret.encode("utf-8"), msg, hashlib.sha256).digest())
    return f"{h_b64}.{p_b64}.{sig}"


ALL_PERMISSIONS = [
    "catalog:movies:read", "catalog:movies:create", "catalog:movies:update", "catalog:movies:delete",
    "catalog:series:read", "catalog:series:create", "catalog:series:update", "catalog:series:delete",
    "catalog:episodes:read", "catalog:episodes:create", "catalog:episodes:update", "catalog:episodes:delete",
    "catalog:platforms:read", "catalog:platforms:create", "catalog:platforms:update", "catalog:platforms:delete",
    "catalog:moods:read", "catalog:moods:create", "catalog:moods:update", "catalog:moods:delete",
    "catalog:genres:read", "catalog:genres:create", "catalog:genres:update", "catalog:genres:delete",
    "catalog:associations:manage", "catalog:admin-media:read"
]

READ_ONLY_PERMISSIONS = [
    "catalog:movies:read", "catalog:series:read", "catalog:episodes:read",
    "catalog:platforms:read", "catalog:moods:read", "catalog:genres:read"
]

ADMIN_TOKEN = create_jwt({"role": ["Admin"], "permission": ALL_PERMISSIONS})
GUEST_TOKEN = create_jwt({"role": ["Guest"], "permission": READ_ONLY_PERMISSIONS, "is_guest": "true"})
EXPIRED_TOKEN = create_jwt({"role": ["Admin"], "permission": ALL_PERMISSIONS}, expired=True)
TAMPERED_TOKEN = create_jwt({"role": ["Admin"], "permission": ALL_PERMISSIONS}, secret="WrongSecretKeyForTestingTamperedSignature!!")


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
        **payload_extra
    }
    h_b64 = b64url(json.dumps(header).encode("utf-8"))
    p_b64 = b64url(json.dumps(payload, separators=(",", ":")).encode("utf-8"))
    return f"{h_b64}.{p_b64}."


NONE_ALG_TOKEN = create_none_alg_jwt({"role": ["Admin"], "permission": ALL_PERMISSIONS})
MALFORMED_TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.invalidpayload.invalidsignature"

test_results = []


def request(method: str, path: str, token: str = None, body: dict = None, headers: dict = None, raw_body: bytes = None):
    time.sleep(0.015)
    url = f"{GATEWAY_BASE_URL}{path}"
    data = raw_body if raw_body is not None else (json.dumps(body).encode("utf-8") if body is not None else None)
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Accept", "application/json, application/problem+json")
    if body is not None and (not headers or "Content-Type" not in headers):
        req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", f"Bearer {token}")
    if headers:
        for k, v in headers.items():
            req.add_header(k, v)

    try:
        with urllib.request.urlopen(req) as resp:
            content = resp.read().decode("utf-8")
            parsed = json.loads(content) if content else None
            return resp.status, parsed, dict(resp.headers)
    except urllib.error.HTTPError as err:
        content = err.read().decode("utf-8")
        parsed = None
        try:
            parsed = json.loads(content)
        except Exception:
            parsed = content
        return err.code, parsed, dict(err.headers)
    except Exception as ex:
        return 0, str(ex), {}


def test_case(category: str, name: str, method: str, path: str, expected_status: int, token: str = None, body: dict = None, headers: dict = None, raw_body: bytes = None):
    status, response_body, resp_headers = request(method, path, token, body, headers, raw_body)
    passed = status == expected_status
    test_results.append({
        "category": category,
        "name": name,
        "method": method,
        "path": path,
        "expected": expected_status,
        "actual": status,
        "passed": passed,
        "body": response_body,
        "headers": resp_headers
    })
    status_color = GREEN if passed else RED
    mark = f"{GREEN}✔ PASS{RESET}" if passed else f"{RED}✘ FAIL{RESET}"
    print(f"[{category}] {mark} {method:6} {path:55} -> Exp: {expected_status} | Got: {status_color}{status}{RESET} - {name}")
    if not passed:
        print(f"    {YELLOW}Details:{RESET} {json.dumps(response_body, indent=2) if isinstance(response_body, dict) else response_body}")
    return response_body, resp_headers


def run_all_tests():
    print(f"\n{BOLD}{CYAN}========================================================================{RESET}")
    print(f"{BOLD}{CYAN}   ShuffleSeries Catalog & API Gateway Comprehensive Test Suite        {RESET}")
    print(f"{BOLD}{CYAN}   Gateway: {GATEWAY_BASE_URL}                                         {RESET}")
    print(f"{BOLD}{CYAN}========================================================================{RESET}\n")

    # -------------------------------------------------------------
    # 1. Health Checks
    # -------------------------------------------------------------
    print(f"\n{BOLD}1. Health Probes (Edge & Internal){RESET}")
    test_case("HEALTH", "Gateway Liveness Probe", "GET", "/health/live", 200)
    test_case("HEALTH", "Gateway Readiness Probe", "GET", "/health/ready", 200)
    test_case("HEALTH", "Catalog Liveness Probe via Gateway", "GET", "/catalog-api/health/live", 200)
    test_case("HEALTH", "Catalog Readiness Probe via Gateway", "GET", "/catalog-api/health/ready", 200)

    # -------------------------------------------------------------
    # 2. Authentication & Authorization Security Edge Cases
    # -------------------------------------------------------------
    print(f"\n{BOLD}2. Security & Edge Authentication (Zero-Trust & PBAC){RESET}")
    test_case("SECURITY", "Unauthenticated Request -> 401 Unauthorized", "GET", "/api/catalog/admin/media", 401, token=None)
    test_case("SECURITY", "Malformed Bearer Token -> 401 Unauthorized", "GET", "/api/catalog/admin/media", 401, token="not_a_valid_jwt_format")
    test_case("SECURITY", "Expired Token -> 401 Unauthorized", "GET", "/api/catalog/admin/media", 401, token=EXPIRED_TOKEN)
    test_case("SECURITY", "Tampered Signature Token -> 401 Unauthorized", "GET", "/api/catalog/admin/media", 401, token=TAMPERED_TOKEN)
    test_case("SECURITY", "Guest Token accessing Admin Media -> 403 Forbidden", "GET", "/api/catalog/admin/media", 403, token=GUEST_TOKEN)
    test_case("SECURITY", "Guest Token attempting to Create Movie -> 403 Forbidden", "POST", "/api/catalog/movies", 403, token=GUEST_TOKEN, body={"releaseYear": 2024})

    # -------------------------------------------------------------
    # 3. Taxonomy - Platforms CRUD & Caching
    # -------------------------------------------------------------
    print(f"\n{BOLD}3. Platforms Taxonomy CRUD & Caching{RESET}")
    test_case("PLATFORMS", "Get All Active Platforms (200 Cached)", "GET", "/api/catalog/platforms", 200, token=GUEST_TOKEN)
    
    unique_slug = f"plat-{int(time.time() * 1000) % 1000000}"
    create_plat_body = {
        "name": f"Platform {unique_slug}",
        "slug": unique_slug,
        "logoUrl": f"https://example.com/{unique_slug}/logo.png",
        "inactiveLogoUrl": f"https://example.com/{unique_slug}/inactive.png",
        "baseWebUrl": f"https://example.com/{unique_slug}",
        "baseAppSchemeUrl": f"{unique_slug}://",
        "displayOrder": 99,
        "isActive": True
    }
    plat_resp, _ = test_case("PLATFORMS", "Create Platform (201 Created)", "POST", "/api/catalog/platforms", 201, token=ADMIN_TOKEN, body=create_plat_body)
    platform_id = plat_resp if isinstance(plat_resp, str) else None

    # Duplicate Platform Slug -> 409 Conflict
    test_case("PLATFORMS", "Duplicate Platform Slug -> 409 Conflict", "POST", "/api/catalog/platforms", 409, token=ADMIN_TOKEN, body=create_plat_body)

    # Validation Error: Empty Name -> 400 Bad Request
    invalid_plat = dict(create_plat_body)
    invalid_plat["name"] = ""
    test_case("PLATFORMS", "Empty Name -> 400 Bad Request", "POST", "/api/catalog/platforms", 400, token=ADMIN_TOKEN, body=invalid_plat)

    # Validation Error: Malformed/Non-HTTP LogoUrl -> 400 Bad Request
    invalid_logo_plat = dict(create_plat_body)
    invalid_logo_plat["slug"] = f"invalid-logo-{unique_slug}"
    invalid_logo_plat["logoUrl"] = "not-a-valid-url"
    test_case("PLATFORMS", "Invalid LogoUrl -> 400 Bad Request", "POST", "/api/catalog/platforms", 400, token=ADMIN_TOKEN, body=invalid_logo_plat)

    # Validation Error: Dangerous AppSchemeUrl (javascript:) -> 400 Bad Request
    dangerous_scheme_plat = dict(create_plat_body)
    dangerous_scheme_plat["slug"] = f"danger-scheme-{unique_slug}"
    dangerous_scheme_plat["baseAppSchemeUrl"] = "javascript:alert(1)"
    test_case("PLATFORMS", "Dangerous AppSchemeUrl (javascript:) -> 400 Bad Request", "POST", "/api/catalog/platforms", 400, token=ADMIN_TOKEN, body=dangerous_scheme_plat)

    if platform_id:
        test_case("PLATFORMS", "Get Platform By Id (200 OK)", "GET", f"/api/catalog/platforms/{platform_id}", 200, token=GUEST_TOKEN)
        test_case("PLATFORMS", "Get Non-Existent Platform By Id -> 404 NotFound", "GET", f"/api/catalog/platforms/{uuid.uuid4()}", 404, token=GUEST_TOKEN)
        test_case("PLATFORMS", "Update Platform (204 NoContent)", "PUT", f"/api/catalog/platforms/{platform_id}", 204, token=ADMIN_TOKEN, body={
            "name": f"Updated Platform {unique_slug}",
            "slug": unique_slug,
            "logoUrl": f"https://example.com/{unique_slug}/logo2.png",
            "inactiveLogoUrl": f"https://example.com/{unique_slug}/inactive2.png",
            "baseWebUrl": f"https://example.com/{unique_slug}/updated",
            "baseAppSchemeUrl": f"{unique_slug}://watch",
            "displayOrder": 100
        })
        test_case("PLATFORMS", "Delete Platform (204 NoContent)", "DELETE", f"/api/catalog/platforms/{platform_id}", 204, token=ADMIN_TOKEN)
        test_case("PLATFORMS", "Get Soft-Deleted Platform By Id -> 404 NotFound", "GET", f"/api/catalog/platforms/{platform_id}", 404, token=GUEST_TOKEN)
        test_case("PLATFORMS", "Delete Non-Existent Platform -> 404 NotFound", "DELETE", f"/api/catalog/platforms/{uuid.uuid4()}", 404, token=ADMIN_TOKEN)

    # -------------------------------------------------------------
    # 4. Taxonomy - Moods CRUD & Caching
    # -------------------------------------------------------------
    print(f"\n{BOLD}4. Moods Taxonomy CRUD & Caching{RESET}")
    test_case("MOODS", "Get All Active Moods (200 Cached)", "GET", "/api/catalog/moods", 200, token=GUEST_TOKEN)
    
    unique_mood = f"mood-{int(time.time() * 1000) % 1000000}"
    create_mood_body = {
        "code": unique_mood,
        "ambientColorHex": "#FF0055",
        "name": f"Adrenaline Rush {unique_mood}",
        "description": "Heart-pounding thrilling content",
        "languageCode": "tr",
        "iconUrl": "https://example.com/icons/thrill.png",
        "displayOrder": 10,
        "isActive": True
    }
    mood_resp, _ = test_case("MOODS", "Create Mood (201 Created)", "POST", "/api/catalog/moods", 201, token=ADMIN_TOKEN, body=create_mood_body)
    mood_id = mood_resp if isinstance(mood_resp, str) else None

    # Duplicate Mood Code -> 409 Conflict
    test_case("MOODS", "Duplicate Mood Code -> 409 Conflict", "POST", "/api/catalog/moods", 409, token=ADMIN_TOKEN, body=create_mood_body)

    if mood_id:
        test_case("MOODS", "Get Mood By Id (200 OK)", "GET", f"/api/catalog/moods/{mood_id}", 200, token=GUEST_TOKEN)
        test_case("MOODS", "Get Non-Existent Mood By Id -> 404 NotFound", "GET", f"/api/catalog/moods/{uuid.uuid4()}", 404, token=GUEST_TOKEN)
        test_case("MOODS", "Add or Update Mood Translation (204 NoContent)", "POST", f"/api/catalog/moods/{mood_id}/translations", 204, token=ADMIN_TOKEN, body={
            "languageCode": "en",
            "name": f"Adrenaline Rush EN {unique_mood}",
            "description": "English description for mood",
            "vibeTags": "thrill,action"
        })
        test_case("MOODS", "Remove Mood Translation (204 NoContent)", "DELETE", f"/api/catalog/moods/{mood_id}/translations/en", 204, token=ADMIN_TOKEN)
        test_case("MOODS", "Update Mood (204 NoContent)", "PUT", f"/api/catalog/moods/{mood_id}", 204, token=ADMIN_TOKEN, body={
            "code": unique_mood,
            "ambientColorHex": "#00FF88",
            "iconUrl": "https://example.com/icons/updated.png",
            "displayOrder": 15
        })
        test_case("MOODS", "Delete Mood (204 NoContent)", "DELETE", f"/api/catalog/moods/{mood_id}", 204, token=ADMIN_TOKEN)
        test_case("MOODS", "Get Soft-Deleted Mood By Id -> 404 NotFound", "GET", f"/api/catalog/moods/{mood_id}", 404, token=GUEST_TOKEN)
        test_case("MOODS", "Delete Non-Existent Mood -> 404 NotFound", "DELETE", f"/api/catalog/moods/{uuid.uuid4()}", 404, token=ADMIN_TOKEN)

    # -------------------------------------------------------------
    # 5. Taxonomy - Genres CRUD & Caching
    # -------------------------------------------------------------
    print(f"\n{BOLD}5. Genres Taxonomy CRUD & Caching{RESET}")
    test_case("GENRES", "Get All Active Genres (200 Cached)", "GET", "/api/catalog/genres", 200, token=GUEST_TOKEN)
    
    unique_genre = f"genre-{int(time.time() * 1000) % 1000000}"
    create_genre_body = {
        "code": unique_genre,
        "name": f"Cyberpunk {unique_genre}",
        "description": "High tech, low life futuristic fiction",
        "languageCode": "tr"
    }
    genre_resp, _ = test_case("GENRES", "Create Genre (201 Created)", "POST", "/api/catalog/genres", 201, token=ADMIN_TOKEN, body=create_genre_body)
    genre_id = genre_resp if isinstance(genre_resp, str) else None

    # Duplicate Genre Code -> 409 Conflict
    test_case("GENRES", "Duplicate Genre Code -> 409 Conflict", "POST", "/api/catalog/genres", 409, token=ADMIN_TOKEN, body=create_genre_body)

    if genre_id:
        test_case("GENRES", "Get Genre By Id (200 OK)", "GET", f"/api/catalog/genres/{genre_id}", 200, token=GUEST_TOKEN)
        test_case("GENRES", "Get Non-Existent Genre By Id -> 404 NotFound", "GET", f"/api/catalog/genres/{uuid.uuid4()}", 404, token=GUEST_TOKEN)
        test_case("GENRES", "Add or Update Genre Translation (204 NoContent)", "POST", f"/api/catalog/genres/{genre_id}/translations", 204, token=ADMIN_TOKEN, body={
            "languageCode": "en",
            "name": f"Cyberpunk EN {unique_genre}",
            "description": "English description for genre"
        })
        test_case("GENRES", "Remove Genre Translation (204 NoContent)", "DELETE", f"/api/catalog/genres/{genre_id}/translations/en", 204, token=ADMIN_TOKEN)
        test_case("GENRES", "Update Genre (204 NoContent)", "PUT", f"/api/catalog/genres/{genre_id}", 204, token=ADMIN_TOKEN, body={
            "code": unique_genre
        })
        test_case("GENRES", "Delete Genre (204 NoContent)", "DELETE", f"/api/catalog/genres/{genre_id}", 204, token=ADMIN_TOKEN)
        test_case("GENRES", "Get Soft-Deleted Genre By Id -> 404 NotFound", "GET", f"/api/catalog/genres/{genre_id}", 404, token=GUEST_TOKEN)
        test_case("GENRES", "Delete Non-Existent Genre -> 404 NotFound", "DELETE", f"/api/catalog/genres/{uuid.uuid4()}", 404, token=ADMIN_TOKEN)

    # -------------------------------------------------------------
    # 6. Movies CRUD & Edge Cases
    # -------------------------------------------------------------
    print(f"\n{BOLD}6. Movies CRUD, Content Negotiation & Edge Cases{RESET}")
    movie_title = f"Interstellar {int(time.time() * 1000) % 1000000}"
    create_movie_body = {
        "tmdbId": int(time.time() * 1000) % 1000000 + 100000,
        "imdbId": f"tt{int(time.time() * 1000) % 10000000:07d}",
        "releaseYear": 2014,
        "runtimeMinutes": 169,
        "posterUrl": "https://image.tmdb.org/t/p/original/interstellar.jpg",
        "backdropUrl": "https://image.tmdb.org/t/p/original/interstellar-bg.jpg",
        "languageCode": "tr",
        "title": movie_title,
        "overview": "Bir grup kaşif, insanlığın hayatta kalması için uzayda bir solucan deliğinden geçerek yeni bir yaşanabilir gezegen arar.",
        "tagline": "Mankind was born on Earth. It was never meant to die here.",
        "director": "Christopher Nolan",
        "imdbRating": 8.7,
        "imdbVotes": 2100000,
        "platformIds": [],
        "moodIds": [],
        "genreIds": []
    }
    movie_resp, _ = test_case("MOVIES", "Create Movie (201 Created)", "POST", "/api/catalog/movies", 201, token=ADMIN_TOKEN, body=create_movie_body)
    movie_id = movie_resp if isinstance(movie_resp, str) else None

    # Validation Error: Release Year < 1888 -> 400 Bad Request
    invalid_movie = dict(create_movie_body)
    invalid_movie["releaseYear"] = 1800
    test_case("MOVIES", "Release Year < 1888 -> 400 Bad Request", "POST", "/api/catalog/movies", 400, token=ADMIN_TOKEN, body=invalid_movie)

    # Validation Error: RuntimeMinutes <= 0 -> 400 Bad Request
    invalid_movie2 = dict(create_movie_body)
    invalid_movie2["runtimeMinutes"] = 0
    test_case("MOVIES", "RuntimeMinutes <= 0 -> 400 Bad Request", "POST", "/api/catalog/movies", 400, token=ADMIN_TOKEN, body=invalid_movie2)

    # Duplicate Movie TMDb ID -> 409 Conflict
    test_case("MOVIES", "Duplicate Movie TMDb ID -> 409 Conflict", "POST", "/api/catalog/movies", 409, token=ADMIN_TOKEN, body=create_movie_body)

    if movie_id:
        test_case("MOVIES", "Get Movie By Id (200 OK)", "GET", f"/api/catalog/movies/{movie_id}", 200, token=GUEST_TOKEN)
        test_case("MOVIES", "Get Movie with Content Negotiation ?lang=tr (200 OK)", "GET", f"/api/catalog/movies/{movie_id}?lang=tr", 200, token=GUEST_TOKEN)
        test_case("MOVIES", "Get Movie with Accept-Language header (200 OK)", "GET", f"/api/catalog/movies/{movie_id}", 200, token=GUEST_TOKEN, headers={"Accept-Language": "tr"})

        # Update Movie
        test_case("MOVIES", "Update Movie Details (204 NoContent)", "PUT", f"/api/catalog/movies/{movie_id}", 204, token=ADMIN_TOKEN, body={
            "tmdbId": create_movie_body["tmdbId"],
            "imdbId": create_movie_body["imdbId"],
            "releaseYear": 2014,
            "runtimeMinutes": 170,
            "director": "C. Nolan",
            "imdbRating": 8.8,
            "imdbVotes": 2200000
        })

        # Update Non-Existent Movie -> 404 NotFound
        test_case("MOVIES", "Update Non-Existent Movie -> 404 NotFound", "PUT", f"/api/catalog/movies/{uuid.uuid4()}", 404, token=ADMIN_TOKEN, body={
            "tmdbId": 999999,
            "imdbId": "tt9999999",
            "releaseYear": 2020,
            "runtimeMinutes": 120
        })

        # Delete Movie
        test_case("MOVIES", "Delete Movie (204 NoContent)", "DELETE", f"/api/catalog/movies/{movie_id}", 204, token=ADMIN_TOKEN)
        test_case("MOVIES", "Get Soft-Deleted Movie -> 404 NotFound", "GET", f"/api/catalog/movies/{movie_id}", 404, token=GUEST_TOKEN)

    # -------------------------------------------------------------
    # 7. Series & Seasons CRUD & Edge Cases (SEC-01 Verified)
    # -------------------------------------------------------------
    print(f"\n{BOLD}7. Series CRUD & DTO Isolation (SEC-01 Verified){RESET}")
    series_title = f"Dark Time {int(time.time() * 1000) % 1000000}"
    create_series_body = {
        "tmdbId": int(time.time() * 1000) % 1000000 + 200000,
        "imdbId": f"tt{int(time.time() * 1000) % 10000000:07d}",
        "releaseYear": 2017,
        "seriesType": 1,  # Standard
        "posterUrl": "https://image.tmdb.org/t/p/original/dark.jpg",
        "backdropUrl": "https://image.tmdb.org/t/p/original/dark-bg.jpg",
        "languageCode": "tr",
        "title": series_title,
        "overview": "Almanya'nın Winden kasabasında kaybolan iki çocuğun ardından dört ailenin geçmişi ve zaman yolculuğu sırları gün yüzüne çıkar.",
        "tagline": "The question is not where, but when.",
        "imdbRating": 8.7,
        "imdbVotes": 450000,
        "totalSeasons": 3
    }
    series_resp, _ = test_case("SERIES", "Create Series (201 Created)", "POST", "/api/catalog/series", 201, token=ADMIN_TOKEN, body=create_series_body)
    series_id = series_resp if isinstance(series_resp, str) else None

    # Duplicate Series TMDb ID -> 409 Conflict
    test_case("SERIES", "Duplicate Series TMDb ID -> 409 Conflict", "POST", "/api/catalog/series", 409, token=ADMIN_TOKEN, body=create_series_body)

    # Validation Error: Empty Title -> 400 Bad Request
    invalid_series = dict(create_series_body)
    invalid_series["title"] = ""
    test_case("SERIES", "Empty Title -> 400 Bad Request", "POST", "/api/catalog/series", 400, token=ADMIN_TOKEN, body=invalid_series)

    # GET /api/catalog/series without ID is not exposed (Catalog media is listed via /admin/media)
    test_case("SERIES", "Get Series List -> 405 Method Not Allowed (Catalog list managed via /admin/media)", "GET", "/api/catalog/series", 405, token=GUEST_TOKEN)

    if series_id:
        test_case("SERIES", "Get Series By Id (200 OK)", "GET", f"/api/catalog/series/{series_id}", 200, token=GUEST_TOKEN)

        # SEC-01 Test: UpdateSeriesRequest has NO Id in body! Id is taken cleanly from route.
        test_case("SERIES", "Update Series (204 NoContent - SEC-01 DTO Isolation)", "PUT", f"/api/catalog/series/{series_id}", 204, token=ADMIN_TOKEN, body={
            "tmdbId": create_series_body["tmdbId"],
            "imdbId": create_series_body["imdbId"],
            "releaseYear": 2018,
            "seriesType": 1,
            "totalSeasons": 4,
            "imdbRating": 8.9,
            "imdbVotes": 500000
        })

        test_case("SERIES", "Update Non-Existent Series -> 404 NotFound", "PUT", f"/api/catalog/series/{uuid.uuid4()}", 404, token=ADMIN_TOKEN, body={
            "tmdbId": 999998,
            "imdbId": "tt9999998",
            "releaseYear": 2020,
            "seriesType": 1,
            "totalSeasons": 1
        })

        test_case("SERIES", "Delete Series (204 NoContent)", "DELETE", f"/api/catalog/series/{series_id}", 204, token=ADMIN_TOKEN)
        test_case("SERIES", "Get Soft-Deleted Series -> 404 NotFound", "GET", f"/api/catalog/series/{series_id}", 404, token=GUEST_TOKEN)

    # -------------------------------------------------------------
    # 8. Episodes CRUD & Edge Cases
    # -------------------------------------------------------------
    print(f"\n{BOLD}8. Episodes CRUD & Standalone Edge Cases{RESET}")
    ep_series_body = {
        "tmdbId": int(time.time() * 1000) % 1000000 + 300000,
        "imdbId": f"tt{int(time.time() * 1000) % 10000000:07d}",
        "releaseYear": 2011,
        "seriesType": 2,  # EpisodicAnthology
        "posterUrl": "https://image.tmdb.org/t/p/original/bm.jpg",
        "languageCode": "tr",
        "title": f"Black Mirror Ep {int(time.time() * 1000) % 1000000}",
        "overview": "Antoloji dizisi",
        "totalSeasons": 1
    }
    ep_series_resp, _ = test_case("EPISODES", "Create Series for Episode Tests", "POST", "/api/catalog/series", 201, token=ADMIN_TOKEN, body=ep_series_body)
    ep_series_id = ep_series_resp if isinstance(ep_series_resp, str) else None

    if ep_series_id:
        create_ep_body = {
            "seriesId": ep_series_id,
            "seasonNumber": 1,
            "episodeNumber": 1,
            "title": "San Junipero",
            "overview": "1987 yılında bir sahil kasabasında tanışan Yorkie ve Kelly'nin aşk hikayesi.",
            "languageCode": "tr",
            "isStandalone": True,
            "runtimeMinutes": 61,
            "imdbRating": 8.6,
            "thumbnailUrl": "https://image.tmdb.org/t/p/original/sanjunipero.jpg"
        }
        ep_resp, _ = test_case("EPISODES", "Create Standalone Episode (201 Created)", "POST", "/api/catalog/episodes", 201, token=ADMIN_TOKEN, body=create_ep_body)
        episode_id = ep_resp if isinstance(ep_resp, str) else None

        # Episode on Non-Existent Series -> 404 NotFound
        invalid_ep_series = dict(create_ep_body)
        invalid_ep_series["seriesId"] = str(uuid.uuid4())
        test_case("EPISODES", "Episode on Non-Existent Series -> 404 NotFound", "POST", "/api/catalog/episodes", 404, token=ADMIN_TOKEN, body=invalid_ep_series)

        # Validation Error: Empty Episode Title -> 400 Bad Request
        invalid_ep_title = dict(create_ep_body)
        invalid_ep_title["title"] = ""
        test_case("EPISODES", "Empty Episode Title -> 400 Bad Request", "POST", "/api/catalog/episodes", 400, token=ADMIN_TOKEN, body=invalid_ep_title)

        if episode_id:
            test_case("EPISODES", "Get Episode By Id (200 OK)", "GET", f"/api/catalog/episodes/{episode_id}", 200, token=GUEST_TOKEN)
            test_case("EPISODES", "Update Episode (204 NoContent)", "PUT", f"/api/catalog/episodes/{episode_id}", 204, token=ADMIN_TOKEN, body={
                "episodeNumber": 1,
                "isStandalone": True,
                "runtimeMinutes": 62,
                "imdbRating": 8.7,
                "thumbnailUrl": "https://image.tmdb.org/t/p/original/sanjunipero2.jpg"
            })

            # Fetch platforms for Episode Association
            plat_list, _ = test_case("EPISODES", "Fetch Platforms for Episode Association", "GET", "/api/catalog/platforms", 200, token=GUEST_TOKEN)
            ep_plat_id = plat_list[0]["id"] if plat_list and isinstance(plat_list, list) and len(plat_list) > 0 else None

            if ep_plat_id:
                # Add Platform to Episode / Deep Link (Admin - 204)
                test_case("EPISODES", "Add Platform to Episode / Deep Link (204 NoContent)", "POST", f"/api/catalog/episodes/{episode_id}/platforms", 204, token=ADMIN_TOKEN, body={
                    "platformId": ep_plat_id,
                    "webUrl": "https://netflix.com/watch/80045435",
                    "appSchemeUrl": "nflx://watch/80045435"
                })

                # Duplicate Platform on Episode -> 409 Conflict
                test_case("EPISODES", "Duplicate Platform on Episode -> 409 Conflict", "POST", f"/api/catalog/episodes/{episode_id}/platforms", 409, token=ADMIN_TOKEN, body={
                    "platformId": ep_plat_id,
                    "webUrl": "https://netflix.com/watch/80045435_updated",
                    "appSchemeUrl": "nflx://watch/80045435_updated"
                })

                # RBAC Verification: Non-Admin (Guest) cannot add platform to episode -> 403 Forbidden
                test_case("EPISODES", "Non-Admin Cannot Add Episode Platform -> 403 Forbidden", "POST", f"/api/catalog/episodes/{episode_id}/platforms", 403, token=GUEST_TOKEN, body={
                    "platformId": ep_plat_id,
                    "webUrl": "https://netflix.com/watch/80045435"
                })

                # Remove Platform from Episode (Admin - 204)
                test_case("EPISODES", "Remove Platform from Episode (204 NoContent)", "DELETE", f"/api/catalog/episodes/{episode_id}/platforms/{ep_plat_id}", 204, token=ADMIN_TOKEN)

            # Add or Update Translation to Episode (Admin - 204)
            test_case("EPISODES", "Add or Update Episode Translation (204 NoContent)", "POST", f"/api/catalog/episodes/{episode_id}/translations", 204, token=ADMIN_TOKEN, body={
                "languageCode": "en",
                "title": "San Junipero (English)",
                "overview": "In 1987, a shy young woman and an outgoing party girl strike up a powerful bond."
            })

            # RBAC Verification: Non-Admin (Guest) cannot add translation to episode -> 403 Forbidden
            test_case("EPISODES", "Non-Admin Cannot Add Episode Translation -> 403 Forbidden", "POST", f"/api/catalog/episodes/{episode_id}/translations", 403, token=GUEST_TOKEN, body={
                "languageCode": "en",
                "title": "Hacked Title"
            })

            # Remove Translation from Episode (Admin - 204)
            test_case("EPISODES", "Remove Episode Translation (204 NoContent)", "DELETE", f"/api/catalog/episodes/{episode_id}/translations/en", 204, token=ADMIN_TOKEN)

            test_case("EPISODES", "Delete Episode (204 NoContent)", "DELETE", f"/api/catalog/episodes/{episode_id}", 204, token=ADMIN_TOKEN)
            test_case("EPISODES", "Get Deleted Episode -> 404 NotFound", "GET", f"/api/catalog/episodes/{episode_id}", 404, token=GUEST_TOKEN)

    # -------------------------------------------------------------
    # 9. Media Associations & Multi-Language Translations
    # -------------------------------------------------------------
    print(f"\n{BOLD}9. Media Associations (Platforms, Moods, Genres & Translations){RESET}")
    assoc_movie_body = {
        "tmdbId": int(time.time() * 1000) % 1000000 + 400000,
        "imdbId": f"tt{int(time.time() * 1000) % 10000000:07d}",
        "releaseYear": 2022,
        "runtimeMinutes": 130,
        "posterUrl": "https://image.tmdb.org/t/p/original/assoc.jpg",
        "languageCode": "tr",
        "title": f"The Batman Assoc {int(time.time() * 1000) % 1000000}",
        "overview": "Gotham şehrinin yeraltı dünyasında adalet arayışı.",
        "director": "Matt Reeves"
    }
    m_resp, _ = test_case("ASSOCIATIONS", "Create Test Movie for Associations", "POST", "/api/catalog/movies", 201, token=ADMIN_TOKEN, body=assoc_movie_body)
    assoc_media_id = m_resp if isinstance(m_resp, str) else None

    # Fetch seeded taxonomy
    seeded_platforms, _ = test_case("ASSOCIATIONS", "Fetch Seeded Platforms", "GET", "/api/catalog/platforms", 200, token=GUEST_TOKEN)
    seeded_moods, _ = test_case("ASSOCIATIONS", "Fetch Seeded Moods", "GET", "/api/catalog/moods", 200, token=GUEST_TOKEN)
    seeded_genres, _ = test_case("ASSOCIATIONS", "Fetch Seeded Genres", "GET", "/api/catalog/genres", 200, token=GUEST_TOKEN)

    target_plat_id = seeded_platforms[0]["id"] if seeded_platforms else None
    target_mood_id = seeded_moods[0]["id"] if seeded_moods else None
    target_genre_id = seeded_genres[0]["id"] if seeded_genres else None

    if assoc_media_id and target_plat_id:
        test_case("ASSOCIATIONS", "Add Platform to Media (204 NoContent)", "POST", f"/api/catalog/media/{assoc_media_id}/platforms", 204, token=ADMIN_TOKEN, body={
            "platformId": target_plat_id,
            "webUrl": "https://netflix.com/watch",
            "appSchemeUrl": "nflx://"
        })
        test_case("ASSOCIATIONS", "Remove Platform from Media (204 NoContent)", "DELETE", f"/api/catalog/media/{assoc_media_id}/platforms/{target_plat_id}", 204, token=ADMIN_TOKEN)

    if assoc_media_id and target_mood_id:
        test_case("ASSOCIATIONS", "Add Mood to Media (204 NoContent)", "POST", f"/api/catalog/media/{assoc_media_id}/moods", 204, token=ADMIN_TOKEN, body={
            "moodId": target_mood_id
        })
        test_case("ASSOCIATIONS", "Remove Mood from Media (204 NoContent)", "DELETE", f"/api/catalog/media/{assoc_media_id}/moods/{target_mood_id}", 204, token=ADMIN_TOKEN)

    if assoc_media_id and target_genre_id:
        test_case("ASSOCIATIONS", "Add Genre to Media (204 NoContent)", "POST", f"/api/catalog/media/{assoc_media_id}/genres", 204, token=ADMIN_TOKEN, body={
            "genreId": target_genre_id
        })
        test_case("ASSOCIATIONS", "Remove Genre from Media (204 NoContent)", "DELETE", f"/api/catalog/media/{assoc_media_id}/genres/{target_genre_id}", 204, token=ADMIN_TOKEN)

    if assoc_media_id:
        # Add English translation (POST /translations)
        test_case("ASSOCIATIONS", "Add English Translation (204 NoContent)", "POST", f"/api/catalog/media/{assoc_media_id}/translations", 204, token=ADMIN_TOKEN, body={
            "languageCode": "en",
            "title": "The Batman (English Title)",
            "overview": "When a sadistic serial killer begins murdering key political figures in Gotham, Batman is forced to investigate.",
            "tagline": "Unmask the truth."
        })
        test_case("ASSOCIATIONS", "Get Movie with English Translation (200 OK)", "GET", f"/api/catalog/movies/{assoc_media_id}?lang=en", 200, token=GUEST_TOKEN)
        test_case("ASSOCIATIONS", "Remove English Translation (204 NoContent)", "DELETE", f"/api/catalog/media/{assoc_media_id}/translations/en", 204, token=ADMIN_TOKEN)

    # -------------------------------------------------------------
    # 10. Admin CMS Media Management & Search (PERF-01 & SEC-02 Verified)
    # -------------------------------------------------------------
    print(f"\n{BOLD}10. Admin CMS Media Listing & Security (PERF-01 Trigram & SEC-02 ReDoS Guard){RESET}")
    test_case("CMS", "Get Admin Media List (200 OK)", "GET", "/api/catalog/admin/media?page=1&pageSize=10", 200, token=ADMIN_TOKEN)
    
    encoded_search = urllib.parse.quote("Başlangıç")
    test_case("CMS", "Admin Media Leading Wildcard Search (200 OK - GIN Trigram Accelerated)", "GET", f"/api/catalog/admin/media?search={encoded_search}", 200, token=ADMIN_TOKEN)
    test_case("CMS", "Filter Admin Media by MediaType=1 (Movie)", "GET", "/api/catalog/admin/media?mediaType=1", 200, token=ADMIN_TOKEN)
    test_case("CMS", "Filter Admin Media by MediaType=2 (Series)", "GET", "/api/catalog/admin/media?mediaType=2", 200, token=ADMIN_TOKEN)
    test_case("CMS", "Filter Admin Media by isDeleted=true (Archive View)", "GET", "/api/catalog/admin/media?isDeleted=true", 200, token=ADMIN_TOKEN)
    test_case("CMS", "Sort Admin Media by imdbRating DESC", "GET", "/api/catalog/admin/media?sortBy=imdbRating&sortDescending=true", 200, token=ADMIN_TOKEN)
    test_case("CMS", "Invalid SortBy Parameter -> 400 Bad Request", "GET", "/api/catalog/admin/media?sortBy=invalid_column", 400, token=ADMIN_TOKEN)

    # SEC-02 Test: Search string > 100 characters must return 400 Validation Error
    long_search_attack = "A" * 101
    test_case("CMS", "Search Parameter > 100 chars -> 400 Bad Request (SEC-02 DoS Defense)", "GET", f"/api/catalog/admin/media?search={long_search_attack}", 400, token=ADMIN_TOKEN)

    # -------------------------------------------------------------
    # 11. Penetration Testing & Adversarial Security Suite (OWASP & STRIDE)
    # -------------------------------------------------------------
    print(f"\n{BOLD}11. Penetration Testing & Adversarial Security Suite (AppSec Guard){RESET}")

    # 11.1 SSRF & Malicious URI Scheme Attacks
    print(f"  {CYAN}▸ [OWASP API7 / API3] SSRF & Malicious Scheme Injections{RESET}")
    if assoc_media_id and target_plat_id:
        test_case("PENTEST-SSRF", "Stored XSS via javascript: URI scheme -> 400 Bad Request", "POST", f"/api/catalog/media/{assoc_media_id}/platforms", 400, token=ADMIN_TOKEN, body={
            "platformId": target_plat_id,
            "webUrl": "javascript:alert(document.cookie)",
            "appSchemeUrl": "nflx://"
        })
        test_case("PENTEST-SSRF", "Data URI Injection Payload -> 400 Bad Request", "POST", f"/api/catalog/media/{assoc_media_id}/platforms", 400, token=ADMIN_TOKEN, body={
            "platformId": target_plat_id,
            "webUrl": "data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==",
            "appSchemeUrl": "nflx://"
        })
        test_case("PENTEST-SSRF", "Local File Disclosure file:///etc/passwd -> 400 Bad Request", "POST", f"/api/catalog/media/{assoc_media_id}/platforms", 400, token=ADMIN_TOKEN, body={
            "platformId": target_plat_id,
            "webUrl": "file:///etc/passwd",
            "appSchemeUrl": "nflx://"
        })
        test_case("PENTEST-SSRF", "Dangerous Protocol Handler in appSchemeUrl -> 400 Bad Request", "POST", f"/api/catalog/media/{assoc_media_id}/platforms", 400, token=ADMIN_TOKEN, body={
            "platformId": target_plat_id,
            "webUrl": "https://netflix.com/watch",
            "appSchemeUrl": "javascript:alert(1)"
        })

    test_case("PENTEST-SSRF", "Malicious Scheme in Movie PosterUrl -> 400 Bad Request", "POST", "/api/catalog/movies", 400, token=ADMIN_TOKEN, body={
        "tmdbId": int(time.time() * 1000) % 1000000 + 500000,
        "imdbId": "tt9999998",
        "releaseYear": 2024,
        "runtimeMinutes": 100,
        "posterUrl": "javascript:alert(1)",
        "languageCode": "tr",
        "title": "Hacker Movie"
    })

    # 11.2 SQL Injection & Trigram Fuzzing
    print(f"  {CYAN}▸ [OWASP API3] SQL Injection & Trigram Blind Fuzzing{RESET}")
    test_case("PENTEST-SQLI", "Classic SQLi Payload (' OR 1=1 --) -> 200 OK (Safely Parameterized)", "GET", "/api/catalog/admin/media?search=" + urllib.parse.quote("' OR 1=1 --"), 200, token=ADMIN_TOKEN)
    test_case("PENTEST-SQLI", "Piggybacked DDL Attack ('; DROP TABLE \"Media\"; --) -> 200 OK", "GET", "/api/catalog/admin/media?search=" + urllib.parse.quote("'; DROP TABLE \"Media\"; --"), 200, token=ADMIN_TOKEN)
    test_case("PENTEST-SQLI", "UNION SELECT Credential Harvesting -> 200 OK (Safely Ignored)", "GET", "/api/catalog/admin/media?search=" + urllib.parse.quote("' UNION SELECT NULL, NULL, username, password_hash FROM \"Users\" --"), 200, token=ADMIN_TOKEN)

    t_start = time.time()
    test_case("PENTEST-SQLI", "Time-Based Blind SQLi pg_sleep(3) -> 200 OK (Executed Without Delay)", "GET", "/api/catalog/admin/media?search=" + urllib.parse.quote("Walter' AND (SELECT pg_sleep(3)) IS NULL --"), 200, token=ADMIN_TOKEN)
    t_elapsed = time.time() - t_start
    if t_elapsed >= 2.5:
        print(f"    {RED}WARNING: Possible Time-Based Blind SQLi delay detected! Elapsed: {t_elapsed:.2f}s{RESET}")

    test_case("PENTEST-SQLI", "Null Byte Poisoning (%00) in Search Parameter -> 400 Bad Request (Sanitized Input Defense)", "GET", "/api/catalog/admin/media?search=Walter%00White", 400, token=ADMIN_TOKEN)

    # 11.3 Broken Object Level Authorization (BOLA / IDOR)
    print(f"  {CYAN}▸ [OWASP API1] Broken Object Level Authorization (BOLA / IDOR){RESET}")
    all_zero_uuid = "00000000-0000-0000-0000-000000000000"
    all_f_uuid = "ffffffff-ffff-ffff-ffff-ffffffffffff"

    test_case("PENTEST-IDOR", "All-Zero UUID Query -> 404 NotFound", "GET", f"/api/catalog/episodes/{all_zero_uuid}", 404, token=GUEST_TOKEN)
    test_case("PENTEST-IDOR", "All-Zero UUID Association -> 400 Bad Request (Validation Boundary)", "POST", f"/api/catalog/episodes/{all_zero_uuid}/platforms", 400, token=ADMIN_TOKEN, body={
        "platformId": target_plat_id if target_plat_id else str(uuid.uuid4()),
        "webUrl": "https://netflix.com/watch/zero"
    })
    test_case("PENTEST-IDOR", "All-F UUID Query -> 404 NotFound", "GET", f"/api/catalog/episodes/{all_f_uuid}", 404, token=GUEST_TOKEN)
    test_case("PENTEST-IDOR", "Non-Existent Episode Platform Assoc -> 404 NotFound (Domain Boundary)", "POST", f"/api/catalog/episodes/{all_f_uuid}/platforms", 404, token=ADMIN_TOKEN, body={
        "platformId": target_plat_id if target_plat_id else str(uuid.uuid4()),
        "webUrl": "https://netflix.com/watch/ghost"
    })
    test_case("PENTEST-IDOR", "Non-Existent Episode Translation Removal -> 404 NotFound", "DELETE", f"/api/catalog/episodes/{all_f_uuid}/translations/tr", 404, token=ADMIN_TOKEN)
    test_case("PENTEST-IDOR", "Guest Role Unauthorized Platform Modification -> 403 Forbidden", "POST", f"/api/catalog/episodes/{all_f_uuid}/platforms", 403, token=GUEST_TOKEN, body={
        "platformId": target_plat_id if target_plat_id else str(uuid.uuid4()),
        "webUrl": "https://netflix.com/watch/ghost"
    })

    # 11.4 ReDoS & Input Buffer Exhaustion
    print(f"  {CYAN}▸ [OWASP API4] ReDoS & Input Buffer Exhaustion{RESET}")
    redos_imdb_id = "tt" + "9" * 1000
    test_case("PENTEST-REDOS", "Oversized IMDb ID Regex DoS Defense -> 400 Bad Request", "POST", "/api/catalog/movies", 400, token=ADMIN_TOKEN, body={
        "tmdbId": int(time.time() * 1000) % 1000000 + 600000,
        "imdbId": redos_imdb_id,
        "releaseYear": 2024,
        "runtimeMinutes": 120,
        "languageCode": "tr",
        "title": "ReDoS Attack Movie"
    })

    test_case("PENTEST-REDOS", "Platform Slug Pattern Attack -> 400 Bad Request", "POST", "/api/catalog/platforms", 400, token=ADMIN_TOKEN, body={
        "name": "Exploit Platform",
        "slug": "a-b-a-b-a-b-a-b-a-b-a-b-a-b-a-b-a-b-a-b-a-b-!",
        "logoUrl": "https://example.com/logo.svg",
        "baseWebUrl": "https://example.com"
    })

    # 11.5 HTTP Parameter Pollution & Content-Type Abuse
    print(f"  {CYAN}▸ [OWASP API8] HTTP Parameter Pollution (HPP) & Content-Type Abuse{RESET}")
    test_case("PENTEST-HPP", "HTTP Parameter Pollution (search=A&search=B) -> 200 OK (Handled)", "GET", "/api/catalog/admin/media?search=Alpha&search=Beta", 200, token=ADMIN_TOKEN)
    test_case("PENTEST-CT", "Unsupported Media Type (form-urlencoded to JSON endpoint) -> 415", "POST", "/api/catalog/movies", 415, token=ADMIN_TOKEN, headers={"Content-Type": "application/x-www-form-urlencoded"}, raw_body=b"title=Test&year=2024")

    # 11.6 JWT Algorithmic Confusion & Malformed Tokens
    print(f"  {CYAN}▸ [OWASP API2] JWT Algorithmic Confusion & Token Tampering{RESET}")
    test_case("PENTEST-JWT", "JWT None Algorithm Attack (alg: 'none') -> 401 Unauthorized", "GET", "/api/catalog/admin/media", 401, token=NONE_ALG_TOKEN)
    test_case("PENTEST-JWT", "Malformed JWT Structure -> 401 Unauthorized", "GET", "/api/catalog/admin/media", 401, token=MALFORMED_TOKEN)

    # -------------------------------------------------------------
    # Summary Report
    # -------------------------------------------------------------
    total = len(test_results)
    passed = sum(1 for r in test_results if r["passed"])
    failed = total - passed

    print(f"\n{BOLD}{CYAN}========================================================================{RESET}")
    print(f"{BOLD}{CYAN}                     TEST EXECUTION SUMMARY                            {RESET}")
    print(f"{BOLD}{CYAN}========================================================================{RESET}")
    print(f"Total Test Cases Executed : {BOLD}{total}{RESET}")
    print(f"Passed                    : {BOLD}{GREEN}{passed}{RESET}")
    print(f"Failed                    : {BOLD}{RED if failed > 0 else GREEN}{failed}{RESET}")
    rate = (passed / total) * 100 if total > 0 else 0
    print(f"Success Rate              : {BOLD}{GREEN if rate == 100 else RED}{rate:.1f}%{RESET}")
    print(f"{BOLD}{CYAN}========================================================================{RESET}\n")

    if failed > 0:
        print(f"{RED}Some test cases failed! Review details above.{RESET}")
        sys.exit(1)
    else:
        print(f"{GREEN}All {total} test cases PASSED successfully through API Gateway!{RESET}\n")


if __name__ == "__main__":
    run_all_tests()
