---
trigger: always_on
description: Threat modeling and OWASP API Security verification across microservices endpoints, auth flows, and data stores.
---

## Threat Modeling & Security Guard (STRIDE & OWASP API Top 10)

Adopt the mindset of an AppSec security architect: Protect trust boundaries, isolate identity, and eliminate attack vectors before code lands in production.

### Core Security Mandates
1. **Trust Boundary & IDOR Defense (OWASP API1/API2):**
   - User and Tenant IDs must strictly be resolved from verified JWT claims (`User.GetUserId()`), never trusted from URL route parameters or request body.
   - Always verify that the requested resource belongs to the calling user in the handler/persistence layer.
2. **Authentication & Token Integrity (OWASP API2):**
   - Refresh token rotation with cryptographic SHA-256 hash chaining (`ReplacedByTokenHash`).
   - Revoked tokens and compromised user sessions must be blocked via the two-tiered Redis Blacklist (`blacklist:token:{jti}` and `blacklist:user:{userId}`).
   - Social logins (Google/Apple) must verify `email_verified == "true"` to prevent Pre-Account Takeover.
   - Use constant-time dummy hashing (`DummyPasswordHash`) to defeat timing attack user enumeration.
3. **Data & Payload Sanitization (OWASP API3/API4):**
   - Never expose Domain entities directly in API endpoints; use strictly validated (FluentValidation) request DTOs to block Mass Assignment.
   - Prevent SQL/NoSQL injection via EF Core parameterized queries; never concatenate user input into raw SQL.
   - Restrict pagination (`MaxPageSize = 100`) and enforce YARP rate limiting to prevent Denial of Service.
4. **Secrets & Information Disclosure (OWASP API8):**
   - Zero hardcoded secrets: All credentials reside in root `.env` (SSOT) and HashiCorp Vault.
   - Never expose stack traces or internal exception details to clients; strictly return sanitized RFC 7807 `ProblemDetails`.
   - Mask sensitive fields (passwords, tokens, payment data) in Serilog via destructuring policies.
