# Task 2.2: Identity Service Geliştirmesi (Authentication, Authorization & Account Lifecycle)

## Ne Yaptık?
Bu görev kapsamında, ShuffleSeries ekosisteminin kimlik doğrulama, kullanıcı yetkilendirme ve hesap yaşam döngüsü yönetimini üstlenen **`ShuffleSeries.Identity`** mikroservisi 5 katmanlı Onion Architecture (Clean Architecture) ve Domain-Driven Design (DDD) standartlarına uygun olarak sıfırdan inşa edildi.

Gerçekleştirilen temel bileşenler ve özellikler:
1. **Domain Modeli (`ShuffleSeries.Identity.Domain`):**
   * **`User` (Aggregate Root):** E-posta/şifre, misafir (`UserStatus.Guest`) ve sosyal hesap durumlarını yöneten, domain event'leri (`UserRegisteredDomainEvent`, `UserMergedDomainEvent`, `UserAccountDeletedDomainEvent`) üreten zengin domain varlığı.
   * **Hibrit Yetkilendirme Modeli (RBAC + PBAC):** Roller (`Role`), izinler (`Permission`), kullanıcı-rol ilişkileri (`UserRole`), rol-izin ilişkileri (`RolePermission`) ve kullanıcı bazlı doğrudan izin ezme (`UserPermission` - IsGranted true/false) entity'leri.
   * **Harici Hesap Eşleme (`UserLogins`):** Google ve Apple SSO hesaplarını tekil `(Provider, ProviderKey)` indeksiyle kullanıcıya bağlayan, Apple token iptali için refresh token saklayabilen bağımsız tablo.
   * **Oturum Güvenliği (`RefreshTokens`):** Kriptografik SHA-256 token özetleri, rotasyon geçmişi (`ReplacedByTokenHash`) ve istemci IP adresini tutan token varlığı.

2. **Uygulama Katmanı ve CQRS (`ShuffleSeries.Identity.Application`):**
   * `RegisterCommand`, `LoginCommand`, `RefreshTokenCommand`, `RevokeTokenCommand`, `CreateGuestSessionCommand`, `SocialLoginCommand`, `MergeGuestAccountCommand`, `DeleteAccountCommand` command handler'ları ve ilgili `FluentValidation` doğrulayıcıları.
   * `GetCurrentUserQuery` ile anlık kimlik ve claim sorgulama.
   * `ITokenService`, `IPasswordHasher` (PBKDF2 SHA-512, 100.000 iterasyon) ve `IPermissionResolver` soyutlamaları.

3. **Altyapı Katmanı (`ShuffleSeries.Identity.Infrastructure`):**
   * `IdentityDbContext`: Soft delete filtreleri, PostgreSQL kısmi indeksleri (silinmemiş mailler için tekil `NormalizedEmail`), cascade silme kuralları ve Transactional Outbox altyapısı.
   * `JwtTokenService`: `System.IdentityModel.Tokens.Jwt` ile HMAC-SHA256 imzalı, claim bazlı JWT ve kriptografik rastgele Refresh Token üretimi.
   * `PermissionResolver`: Kullanıcının sahip olduğu tüm aktif rollerden türetilen izinleri toplayan ve kullanıcıya özel doğrudan atamalarla (`UserPermission.IsGranted`) harmanlayarak dinamik claim listesi çıkaran motor.
   * `ExternalAuthService`: Google & Apple ID token doğrulama simülasyonu ve harici sağlayıcı eşleme altyapısı.
   * Quartz tabanlı arka plan Outbox işleyicisi (`ProcessOutboxMessagesJob`).

4. **API Katmanı (`ShuffleSeries.Identity.Api`):**
   * `/api/auth` prefix'i altında Minimal API endpoint'leri (`register`, `login`, `refresh`, `revoke`, `guest`, `social-login`, `merge-guest`, `account`, `me`).
   * HashiCorp Vault, Serilog, OpenTelemetry, Health Checks ve Shared JWT middleware entegrasyonları.
   * Uygulama açılışında veritabanı migration'ını ve varsayılan rol/izin tohumlamasını (`Guest`, `Standard`, `Premium`, `Admin`) yapan otomatik `MigrationExtensions`.

5. **API Gateway Entegrasyonu (`ShuffleSeries.ApiGateway`):**
   * YARP küme (`identity-cluster`) tanımları ve `/identity-api/health/*` ile `/identity-api/openapi/*` ters vekil (reverse proxy) yönlendirmeleri.
   * Gateway'de endpoint bazlı JSON şişkinliğini önleyen rota mimarisi ve claim tabanlı doğrulama altyapısı.

---

## Neden Yaptık? (Mimari Kararlar)

* **Neden Salt Role (RBAC) veya Salt Claim Yerine Hibrit Model (RBAC + PBAC) Seçtik?**
  * Yalnızca rol tabanlı yetkilendirme (RBAC) kullanıldığında, tek bir özelliğe erişim vermek için yüzlerce yeni rol kombinasyonu (`StandardUserWithCatalogAccess`, vb.) türetilir (Role Explosion).
  * Yalnızca izin/claim tabanlı modelde (PBAC/ABAC) ise her yeni kullanıcıya onlarca iznin tek tek atanması gerekir, bu da veri tabanında gereksiz kayıt yükü ve yönetim zorluğu yaratır.
  * Hibrit modelde roller (`Standard`, `Premium`) izin demetleri olarak işlev görür. Kullanıcıya doğrudan bir rol atanır; gerekirse tekil bir kullanıcıya sistem yöneticisi tarafından istisnai bir izin verilebilir veya geri alınabilir (`UserPermission`). Bu izinler JWT içine claim olarak gömülür, böylece API Gateway ve servisler veritabanına sorgu atmadan token claim'i üzerinden anlık mikro yetkilendirme yapabilir.

* **Neden Sosyal Girişleri `Users` Tablosunda Tutmak Yerine `UserLogins` Tablosuna Ayırdık?**
  * Kullanıcıların hem Google hem Apple ile aynı hesaba bağlanabilmesi (Account Linking) için 1-N ilişki zorunludur.
  * Apple App Store Guideline 5.1.1(v) gereğince, Apple ile giriş sunan uygulamalar kullanıcı hesabı silindiğinde Apple sunucularına bir istek atarak kullanıcının yetkilendirmesini iptal etmek (Revoke Token) zorundadır. Bunun için Apple'dan alınan özel token'ların güvenle saklanması gerekir. `UserLogins` tablosu bu hassas sağlayıcı metaverilerini ana kullanıcı profilinden izole eder.

* **Neden Refresh Token Rotasyonu (RTR) ve Yeniden Kullanım Tespiti (Reuse Detection) Uyguladık? (OWASP API4)**
  * Klasik sabit refresh token'lar çalındığında saldırgan sonsuza kadar yeni JWT üretebilir.
  * Rotasyon stratejisinde her refresh işleminde eski token anında iptal edilir (`RevokedAtUtc`), yerine yeni bir token üretilir ve aralarındaki bağ `ReplacedByTokenHash` ile zincirlenir.
  * Eğer iptal edilmiş bir refresh token ile tekrar istek yapılırsa, bu bir saldırı girişimi (token hırsızlığı) olarak algılanır ve o kullanıcıya ait **tüm aktif refresh token'lar anında geçersiz kılınarak (Token Family Revocation)** oturum sonlandırılır.

* **Neden Misafir Oturumu (Guest Session) ve Data Merge Mimarisi Kurduk?**
  * Modern mobil uygulamalarda kullanıcıyı kayıt formlarıyla karşılamak dönüşüm oranlarını (drop-off) ciddi oranda düşürür.
  * Misafir oturumu (`POST /api/auth/guest`), kullanıcıya şifresiz, anında geçici bir JWT ve başlangıç biletleri verir. Kullanıcı uygulamayı dener, içerikleri beğenir veya kaydeder.
  * Kullanıcı daha sonra kalıcı hesaba geçtiğinde (`POST /api/auth/merge-guest`), misafir oturumundaki tüm veriler atomik olarak kalıcı hesaba aktarılır ve `UserMergedDomainEvent` fırlatılır.

---

## Hangi Pattern'leri Kullandık?

* **Domain-Driven Design (DDD) & Aggregate Root:** `User` varlığı tüm kuralları (şifre değiştirme, durum güncelleme, token yönetimi, login ekleme) kendi içinde kapsüller (encapsulation).
* **CQRS (Command Query Responsibility Segregation) & MediatR:** Okuma (`GetCurrentUserQuery`) ve yazma (`RegisterCommand`, `LoginCommand`, vb.) işlemleri ayrık modeller ve handler'lar ile yönetilir.
* **Hybrid RBAC + PBAC Pattern:** Rollerin izin şablonu olarak çalıştığı, kullanıcı bazlı ezme kurallarının işletildiği esnek yetkilendirme mimarisi.
* **Token Rotation & Token Family Invalidation:** OWASP API4 token çalınması koruma deseni.
* **Domain Events & Transactional Outbox Pattern:** Hesap olayları (`UserRegisteredDomainEvent`, `UserMergedDomainEvent`, `UserAccountDeletedDomainEvent`) veritabanı transaction'ı ile birlikte `OutboxMessages` tablosuna yazılır; arka plan Quartz servisi tarafından güvenilir şekilde yayınlanır.
* **Strongly-Typed Domain Exceptions & Ubiquitous Language:** Magic string ve generic altyapı exception'ları yerine Domain katmanında modellenen tip güvenli exception sınıfları (`EmailAlreadyInUseException`, `InvalidCredentialsException`, `TokenCompromisedException` vb.) kullanılarak hata yönetimi zenginleştirilmiştir.
* **Repository Marker Interface & Aggregate Root Modeli:** `IRepository` non-generic marker interface'i ile assembly scanning ve NetArchTest kuralları güçlendirilirken; `User`, `Role` ve `Permission` sınıfları birer `AggregateRoot` olarak modellenerek `IRepository<TEntity>` generic sözleşmesiyle tam CRUD yeteneklerine ve Outbox domain event desteğine kavuşturulmuştur.
* **System.TimeProvider Soyutlaması:** Statik `DateTime.UtcNow` çağrıları kaldırılarak .NET 8+ `TimeProvider` soyutlaması üzerinden deterministik ve birim testlerinde sanallaştırılabilir zaman yönetimi sağlandı. Domain varlıkları saf tutularak zaman bilgisi dışarıdan parametre olarak iletildi.
* **AuditableEntityInterceptor & Merkezi Denetim (Time Auditing):** `IAuditableEntity` arayüzü ile `CreatedAtUtc` ve `ModifiedAtUtc` zaman damgaları, EF Core `AuditableEntityInterceptor` (SaveChangesInterceptor) üzerinden `TimeProvider` kullanılarak merkezi ve deterministik olarak yönetildi. `SoftDeleteInterceptor` ve `InsertOutboxMessagesInterceptor` da `TimeProvider` ile senkronize edilerek domain sınıfları saf POCO tutuldu.
* **SystemRoles & SystemPermissions Constants (Single Source of Truth):** Kod içindeki `"Standard"`, `"Guest"`, `"Admin"`, `"catalog:read"` gibi tüm roller ve modüler yetki sabitleri `ShuffleSeries.Shared.Core.Domain.Constants` katmanına taşındı. Identity (sağlayıcı), Catalog (tüketici) ve API Gateway (politika yöneticisi) aynı strongly-typed sabitleri paylaşarak "Distributed Magic String Sprawl" ortadan kaldırıldı.
* **ClaimsPrincipalExtensions (OWASP API1/API2):** `ShuffleSeries.Shared.Core.Web.Extensions` altında `user.GetUserId()`, `user.TryGetUserId()`, `user.GetEmail()`, `user.IsGuest()`, `user.GetRoles()`, `user.GetPermissions()`, `user.HasPermission()` metotları yazıldı. Endpoint seviyesinde ham string çözümleme ve kod tekrarı elendi; geçersiz token'larda RFC 7807 uyumlu `UnauthorizedException` üretildi.
* **IAuthorizationMiddlewareResultHandler & RFC 7807 Standardı:** ASP.NET Core `AuthorizationMiddleware` yetki eksikliğinde exception fırlatmayıp `ForbidAsync` (boş 403) çağırdığından, `ProblemDetailsAuthorizationMiddlewareResultHandler` yazılarak `403 Forbidden` yanıtları standart RFC 7807 ProblemDetails JSON gövdesine (`AUTH_FORBIDDEN`, traceId, correlationId) kavuşturuldu.
* **JwtBearerEvents.OnChallenge & 401 RFC 7807 Standardı:** Kimlik doğrulanmamış veya süresi dolmuş token ile yapılan isteklerde framework'ün boş 401 dönmesi engellendi; `OnChallenge` olayı ile standart `AUTH_UNAUTHORIZED` ProblemDetails gövdesi dönmesi sağlandı.
* **SystemPolicies & Claim Type Alignment:** Yetkilendirme politikaları `SystemPolicies` altında strongly-typed olarak toplandı; token üretimindeki küçük harfli `"permission"` claim tipi ile politikalardaki arama tipi birebir senkronize edildi.

---

## Nasıl Test Edilir?

### 1. Otomatik Test Paketleri
Sistem, Clean Architecture kurallarına ve gerçek veritabanı ortamına göre 3 seviyede test edilmiştir:

```bash
# 1. Identity Servisi İzole Unit Testleri (Domain kuralları, PasswordHasher, Token Rotation, PermissionResolver)
dotnet test ShuffleSeries.Identity/ShuffleSeries.Identity.Tests

# 2. Mimari Bağımlılık Testleri (NetArchTest Onion Architecture & CQRS Kuralları)
dotnet test ShuffleSeries.ArchitectureTests

# 3. Gerçek Entegrasyon Testleri (Testcontainers PostgreSQL + WebApplicationFactory + Respawn)
dotnet test ShuffleSeries.Identity/ShuffleSeries.Identity.IntegrationTests

# 4. Tüm Çözüm Testleri (Tüm servisler - 277 test)
dotnet test
```

### 2. Entegrasyon Test Senaryoları (Test Cases)
* `Register_WithValidCredentials_Returns201CreatedAndTokens`: Yeni kullanıcı kaydında HTTP 201, JWT, Refresh Token ve standart rollerin dönmesi.
* `Register_WithDuplicateEmail_Returns409Conflict`: Aynı e-posta ile mükerrer kayıtta HTTP 409 ve RFC 7807 ProblemDetails dönmesi.
* `Login_WithValidCredentials_Returns200OkAndTokens`: Başarılı girişte JWT, Refresh Token ve claim listesinin dönmesi.
* `Login_WithInvalidPassword_Returns401Unauthorized`: Yanlış şifre denemesinde HTTP 401 dönmesi.
* `RefreshToken_WithValidToken_ReturnsNewTokens`: Geçerli refresh token ile yeni JWT ve yeni refresh token alınması (Rotation).
* `CreateGuestSession_Returns201CreatedAndGuestClaims`: Misafir oturumu talebinde `is_guest: true` claim'li JWT üretilmesi.
* `GetCurrentUser_WithBearerToken_ReturnsUserData`: Korunan `/api/auth/me` endpoint'inin JWT ile kullanıcı bilgilerini dönmesi.
