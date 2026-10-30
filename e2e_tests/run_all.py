#!/usr/bin/env python3
"""
ShuffleSeries - Master End-to-End (E2E) Test Suite Runner
Orchestrates and executes all API Gateway black-box E2E test suites:
1. API Gateway Edge Capabilities & Resilience (test_api_gateway.py)
2. Identity Service E2E Lifecycle & Security (test_identity_api_gateway.py)
3. Catalog Service CQRS, Taxonomies & CMS (test_catalog_api_gateway.py)
"""

import subprocess
import sys
import os
import time

GREEN = "\033[92m"
RED = "\033[91m"
YELLOW = "\033[93m"
BLUE = "\033[94m"
CYAN = "\033[96m"
BOLD = "\033[1m"
RESET = "\033[0m"

SUITES = [
    {
        "name": "API Gateway Edge & Security Suite",
        "file": "test_api_gateway.py",
        "description": "Health Probes, YARP Routing, X-Correlation-ID, CORS, Edge RBAC, Redis Blacklist, Rate Limiting"
    },
    {
        "name": "Identity Service & Auth Suite",
        "file": "test_identity_api_gateway.py",
        "description": "Registration, Login, Profile (/me), Guest Sessions, Conversion, Refresh Rotation, Revocation, Deletion"
    },
    {
        "name": "Catalog Service & Media Suite",
        "file": "test_catalog_api_gateway.py",
        "description": "Taxonomies (Platforms, Moods, Genres), Movies, Series, Episodes, Associations, Translations, CMS Search"
    }
]


def run_all_suites():
    start_time = time.time()
    current_dir = os.path.dirname(os.path.abspath(__file__))

    print(f"\n{BOLD}{BLUE}========================================================================")
    print(f"      ShuffleSeries - Master End-to-End (E2E) Test Orchestrator         ")
    print(f"      Target: YARP API Gateway (http://localhost:5001)                 ")
    print(f"========================================================================{RESET}\n")

    results = []
    overall_exit_code = 0

    for idx, suite in enumerate(SUITES, 1):
        suite_path = os.path.join(current_dir, suite["file"])
        print(f"{BOLD}{CYAN}>>> [{idx}/{len(SUITES)}] Running {suite['name']} ({suite['file']})...{RESET}")
        print(f"    {suite['description']}\n")

        suite_start = time.time()
        process = subprocess.run([sys.executable, suite_path])
        suite_duration = time.time() - suite_start

        success = (process.returncode == 0)
        results.append({
            "name": suite["name"],
            "file": suite["file"],
            "success": success,
            "duration": suite_duration,
            "exit_code": process.returncode
        })

        if not success:
            overall_exit_code = 1

        print(f"\n{'-'*72}\n")

    total_duration = time.time() - start_time

    # Unified Master Summary Table
    print(f"{BOLD}{BLUE}========================================================================")
    print(f"                   MASTER E2E SUITE EXECUTION SUMMARY                  ")
    print(f"========================================================================{RESET}")
    print(f"{'Suite Name':<38} {'Script':<25} {'Result':<10} {'Time':<8}")
    print(f"{'-'*38} {'-'*25} {'-'*10} {'-'*8}")

    for res in results:
        status_text = f"{GREEN}{BOLD}PASSED{RESET}" if res["success"] else f"{RED}{BOLD}FAILED{RESET}"
        time_text = f"{res['duration']:.1f}s"
        print(f"{res['name']:<38} {res['file']:<25} {status_text:<19} {time_text:<8}")

    print(f"{BOLD}{BLUE}========================================================================{RESET}")
    total_passed = sum(1 for r in results if r["success"])
    total_suites = len(results)

    if overall_exit_code == 0:
        print(f"{GREEN}{BOLD}✔ ALL {total_suites} E2E TEST SUITES PASSED! Total Execution Time: {total_duration:.1f}s{RESET}\n")
    else:
        print(f"{RED}{BOLD}✘ {total_suites - total_passed} of {total_suites} SUITES FAILED! Total Execution Time: {total_duration:.1f}s{RESET}\n")

    return overall_exit_code


if __name__ == "__main__":
    sys.exit(run_all_suites())
