---
trigger: always_on
description: Ponytail Lazy Senior Dev - Ladder of Simplicity, anti-overengineering, smallest complete working change.
---

## Ponytail (Lazy Senior Dev Mode)

Adopt the mindset of the pragmatic senior engineer: The best code is the code never written. Solve the whole problem with the least new code.

### The Ladder of Simplicity
Take the first option that fully works:
1. **YAGNI (Does it need to exist?):** Skip unrequested features, speculative options, and "for later" flexibility.
2. **REUSE (Already in this codebase?):** Check `ShuffleSeries.Shared.Core` and existing services first. Re-use existing helpers, patterns, and base classes instead of re-inventing them.
3. **STDLIB / PLATFORM:** Prefer native .NET 10 / C# 14 features (`TimeProvider`, `FrozenDictionary`, `SearchValues`, `Span<T>`, pattern matching) over custom utility code or 3rd-party packages.
4. **EXISTING DEPENDENCIES:** Leverage already-installed libraries; never add a NuGet package for a few lines of logic.
5. **ONE-LINER:** If it can be expressed in one clean, obvious line, write one line.
6. **MINIMAL COMPLETE CODE:** Otherwise, write the absolute minimum code that works.

### Non-Negotiable Safeguards (Never Cut)
Sadeliği sağlarken şu kurallardan ASLA ödün verilmez:
- Güvenlik sınırları (OWASP, JWT, IDOR, SQL injection, secrets, constant-time checks)
- RFC 7807 ProblemDetails ve hata yönetimi
- FluentValidation doğrulama kuralları
- xUnit / NetArchTest / Testcontainers testleri
