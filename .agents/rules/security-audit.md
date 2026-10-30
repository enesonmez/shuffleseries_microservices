---
trigger: always_on
description: Cloudflare 6-phase adversarial security audit and vulnerability discovery harness for deep code reviews, pen-tests, and milestone audits.
---

## Cloudflare Security Audit (Adversarial Vulnerability Discovery)

Use this adversarial auditing framework for deep codebase security reviews, penetration testing, and milestone close-outs (`/security-audit`).

### Dual-Track Security Strategy
- **Track 1 (Pre-Task / Feature Design - Shift-Left):** Use `/threat-model` (STRIDE + OWASP API) to define trust boundaries and prevent vulnerabilities before writing code.
- **Track 2 (Post-Implementation / Milestone Close-Out - Shift-Right):** Use `/security-audit` (Cloudflare Harness) for deep, multi-phase adversarial code auditing.

### 6-Phase Audit Protocol
1. **Reconnaissance:** Map architecture, trust boundaries, ingress surfaces, and generate `coverage-ledger.json`.
2. **Coverage-Led Hunting:** Dispatch isolated hunter agents targeting core attack classes (RPC/Messaging, Data Isolation, Auth, Resource Exhaustion).
3. **Adversarial Validation:** Every candidate finding must be sent to a fresh verifier whose objective is to **disprove** the vulnerability, eliminating false positives.
4. **Structured Output:** Emit `confirmed`, `needs_validation`, and `rejected` records to `findings.json` and validate with `validate-findings.cjs`.
5. **Independent Record Verification:** Fresh agents verify code line anchors in source files.
6. **Target-Neutral Reporting:** Generate `REPORT.md`, `FINDINGS-DETAIL.md`, and `NEEDS-VALIDATION.md`.

### Core Rules
- Only confirm real boundary violations (not checklist deviations or defense-in-depth notes).
- Only `confirmed` records receive severity. `needs_validation` has no severity.
- Propose the narrowest effective source fix at the last trusted decision point.
