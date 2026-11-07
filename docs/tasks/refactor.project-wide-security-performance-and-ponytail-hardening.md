# Proje Geneli Güvenlik, Performans ve Yalınlaştırma İyileştirmeleri (Remediation & Hardening)

Bu doküman; `project_wide_security_performance_ponytail_review.md` raporunda tespit edilen 16 kritik mimari bulgunun, Ponytail (aşırı mühendislikten kaçınma), Threat Modeling (OWASP API Top 10) ve Performance Guard anayasaları doğrultusunda çözüme kavuşturulmasını ve Catalog Transactional Outbox entegrasyonunu belgeler.

---

## 1. Ne Yaptık?

Sistem genelinde 4 fazda yürütülen çalışma ile aşağıdaki 16 iyileştirme gerçekleştirildi:

### Faz 1: Güvenlik Açıkları & Hata Giderme
1. **Dış Kimlik Doğrulama Fail-Closed Koruma (#1):** Sosyal girişlerde (`GoogleAuthProvider`, `AppleAuthProvider`) `ExternalAuth:ValidateSignatures` bayrağının prodüksiyon ortamında `false` yapılması durumunda uygulamanın ayağa kalkmasını engelleyen (`InvalidOperationException`) Fail-Closed güvenlik kilidi konuldu.
2. **Sosyal Girişte Pasif/Askıya Alınmış Kullanıcı Engeli (#2):** `SocialLoginCommandHandler` içinde, mevcut bir sosyal bağlantı varsa veya e-posta üzerinden hesap eşleniyorsa, kullanıcının `Status == UserStatus.Active` olduğu doğrulandı; askıdaki kullanıcıların sosyal token ile içeri sızması engellendi.
3. **API Gateway Rate Limiter Sıralaması & Kimlik Çözümleme (#3):** `ApiGateway/Program.cs` içinde `UseRateLimiter()` middleware'i `UseAuthentication()` sonrasına taşındı. `RateLimitingExtensions.cs` içinde rate limit bölümleme anahtarı olarak `context.User.TryGetUserId()` önceliklendirildi; sahte IP saldırılarına karşı `RemoteIpAddress` veya `X-Forwarded-For` son adımı (rightmost hop) baz alındı. Red durumunda standart RFC 7807 `RATE_LIMIT_EXCEEDED` ProblemDetails dönüldü.
4. **CORS Skaler String Bağlama Koruması (#4):** `CorsExtensions.cs` içinde virgülle veya noktalı virgülle ayrılmış tekil string konfigürasyonları otomatik split edilerek diziye dönüştürüldü; CORS originlerinin sessizce boşa düşmesi engellendi.
5. **Claim İsim Çelişkisi Standardizasyonu (#5):** `ClaimsPrincipalExtensions.cs` içine `public const string PermissionClaimType = "permission"` eklendi ve hem tekil (`permission`) hem çoğul (`permissions`) claim isimleri geriye dönük uyumlu olarak desteklendi.
6. **JWT Zaman Damgası Milisaniye Hassasiyeti (#6):** `JwtTokenService.cs` içinde üretilen token'lara milisaniye cinsinden `iat_ms` claim'i eklendi; `ClaimsPrincipalExtensions.cs` bu değeri çözümleyerek aynı saniye içinde üretilen ve iptal edilen oturumlar arasındaki yarış durumunu (race condition) ortadan kaldırdı.
7. **Redis Token & Session Kara Liste `MGET` Birleştirmesi (#7):** `ITokenBlacklistService` arayüzüne `IsSessionRevokedAsync` metodu eklendi. `RedisTokenBlacklistService.cs` içinde token (`blacklist:token:{jti}`) ve kullanıcı (`blacklist:user:{userId}`) kontrolleri tek bir gidiş-dönüşte (single roundtrip) Redis `StringGetAsync([tokenKey, userKey])` (`MGET`) ile birleştirilerek Gateway gecikmesi yarıya indirildi.
8. **Düz Metin Sır ve YAGNI Temizliği (#13):** `UserLogins` tablosunda ve DTO'larda tutulan kullanılmayan `AppleRefreshToken` alanı API, Application, Domain ve EF Core katmanlarından tamamen silindi; `RemovePlaintextRefreshTokenFromUserLogins` EF Core migration'ı ile veritabanı kolonu düşürüldü.
9. **Bilinmeyen ArgumentException Sızıntısı & Gateway Swagger Koruması (#14):** `GlobalExceptionsHandler.cs` içinde BCL `ArgumentException` yakalayıcısı kaldırılarak bilinmeyen parametre hatalarının hassas iç sistem detayları sızdırması önlendi (RFC 7807 500 INTERNAL_SERVER_ERROR). Gateway'de Swagger UI `if (app.Environment.IsDevelopment())` bloğuna alındı.

### Faz 2: Performans & Ölçeklenebilirlik
10. **UserRepository Eager-Loading Arındırma (#8):** `GetByEmailAsync` ve `GetByLoginAsync` metotlarından gereksiz `.Include(u => u.RefreshTokens)` kaldırıldı; `GetByIdAsync` ve `GetByRefreshTokenHashAsync` metotlarında ise yalnızca aktif ve hedef token'lar filtrelenerek belleğe çekildi.
11. **Sosyal Sağlayıcı JWKS Zaman Aşımı & Esneklik (#9):** `BaseJwtSocialAuthProvider.cs` içinde JWKS indirme isteklerine 10 saniyelik `CancellationTokenSource` ve `IHttpClientFactory` resilience politikası bağlandı.
12. **LoggingBehavior DEBUG Seviyesi İzolasyonu (#10):** Büyük JSON istek ve yanıt gövdelerinin Serilog tarafından yapısal olarak çözümlenmesi (`{@RequestPayload}`, `{@ResponsePayload}`) yalnızca `LogLevel.Debug` seviyesine çekildi. Normal operasyonda `LogLevel.Information` ile yalnızca süre ve talep adı loglandı.
13. **Çoklu Pod Transactional Outbox & 7 Günlük Saklama Purge'ü (#11):**
    - `ShuffleSeries.Shared.Core.Infrastructure.Outbox` altında PostgreSQL `FOR UPDATE SKIP LOCKED` kullanan merkezi `OutboxBatchProcessor.cs` geliştirildi.
    - `ShuffleSeries.Shared.Core.Domain.Events` altında 7 adet bağımsız, sealed Integration Event kontratı (`MediaCreatedEvent`, `MediaUpdatedEvent`, `MediaDeletedEvent`, `EpisodeCreatedEvent`, `EpisodeUpdatedEvent`, `EpisodeDeletedEvent`, `CatalogTaxonomyChangedEvent`) oluşturuldu.
    - `ProcessOutboxMessagesJob` (Catalog ve Identity) bu işlemciye delege edildi; 7 günden eski işlenmiş outbox mesajları `ExecuteDeleteAsync()` ile otomatik silindi.
14. **Catalog & Identity Sorgu Optimizasyonları (#12):**
    - `ISeriesRepository` ve `SeriesRepository`'ye `.AsSplitQuery()` kullanan `GetByIdWithSeasonAsync` eklendi; bölüm eklerken tüm sezon ve bölümlerin ChangeTracker'a yüklenmesi önlendi.
    - `MoodRepository` ve `GenreRepository`'ye salt ID döndüren `GetExistingActiveIdsAsync` metotları eklendi; film/dizi oluştururken tüm taksonomi çevirilerinin çekilmesi engellendi.
    - `MediaRepository.cs` içinde `EF.Functions.ILike` öncesinde SQL wildcard karakterleri (`\`, `%`, `_`) escape edildi.
    - `RoleRepository.cs` ve `PermissionResolver.cs` içine boş rol ID listesi durumunda DB'ye gitmeden $O(1)$ erken dönüş (early return) eklendi.

### Faz 3: Yalınlaştırma (Dead Code Removal) & TimeProvider
15. **Ölü Önbellek Kodlarının ve Gereksiz Paketlerin Temizlenmesi (#15):**
    - `ICatalogCacheService` ve `RedisCatalogCacheService` içerisindeki senkron `void Remove(string key)` metodu silindi (ThreadPool starvation önlendi).
    - Kullanılmayan `MemoryCatalogCacheService.cs` ve birim testleri silindi; `ShuffleSeries.Catalog.Infrastructure.csproj` içinden `Microsoft.Extensions.Caching.Memory` paketi kaldırıldı.
    - `RedisServiceCollectionExtensions.cs` içindeki yan etkili ve gereksiz `AddSharedTokenBlacklist(services, configuration)` overload'u silindi.
16. **Inbox Zaman Damgası Esnekliği (#16):**
    - `InboxMessage.cs` constructor'ına opsiyonel `DateTime? receivedAtUtc = null` parametresi eklendi (`ReceivedAtUtc = receivedAtUtc ?? DateTime.UtcNow;`).

---

## 2. Neden Yaptık?

* **Fail-Closed vs Fail-Open Dengesi:** Canlı ortamda dış kimlik sağlayıcılarının imzalarının atlanabilmesi sahte JWT token üretilmesine zemin hazırlar. Bu nedenle konfigürasyonel imza atlama bayrağı prodüksiyonda çökecek şekilde (Fail-Closed) kilitlendi. Önbellek çöküşlerinde ise veritabanına şeffaf geri düşüş (Fail-Open) sağlandı.
* **YAGNI ve Minimalizm (Ponytail İlkesi):** Gelecekte gerekeceği varsayımıyla (spekülatif) `UserLogins` tablosuna eklenmiş olan `AppleRefreshToken`, şu an hiçbir arka plan işi tarafından kullanılmadığı ve düz metin sır riski taşıdığı için tamamen kaldırıldı. Kod satırları azaltılarak saldırı yüzeyi daraltıldı.
* **PostgreSQL FOR UPDATE SKIP LOCKED İhtiyacı:** Çoklu pod (replica) ortamlarında her mikroservis kendi arka plan Outbox işleyicisini koşturur. Klasik `SELECT ... LIMIT 20` sorgusu aynı anda koşan işleyicilerin aynı mesajları almasına (yarış durumu / race condition) ve RabbitMQ'ya mükerrer mesaj basılmasına yol açar. `FOR UPDATE SKIP LOCKED` satırları kilitlerken diğer pod'ların bu satırları beklemeden bir sonraki kilitsiz satırları almasını sağlar; sıfır gecikme ve sıfır çakışma sunar.
* **Bellek ve GC Baskısının Azaltılması:** MediatR `LoggingBehavior` içinde büyük yüklerin loglanması ve repository sorgularında ihtiyaç duyulmayan 1-N koleksiyonların yüklenmesi (over-fetching), Garbage Collector'ın Gen0/Gen1/Gen2 döngülerini artırarak mikro duraklamalara (micro-stutters) yol açıyordu. Sadece DEBUG seviyesinde yük loglama ve targeted repository metotları ile bellek kullanımı optimize edildi.

---

## 3. Hangi Pattern'leri Kullandık?

| Pattern | Uygulama Yeri | Amacı ve İşlevi |
| :--- | :--- | :--- |
| **Transactional Outbox & Batch Processor** | `OutboxBatchProcessor.cs`, `ProcessOutboxMessagesJob.cs` | DB mutasyonları ile RabbitMQ event fırlatımı arasında atomik tutarlılık; `FOR UPDATE SKIP LOCKED` ile dağıtık kilitsiz eşzamanlılık; `ExecuteDeleteAsync` ile 7 günlük retention purge. |
| **Two-Tier Redis Blacklist with MGET** | `RedisTokenBlacklistService.cs`, `AuthenticationExtensions.cs` | Tekil token (`jti`) ve kullanıcı oturum (`userId` Unix timestamp) kontrollerini tek bir Redis gidiş-dönüşünde ($O(1)$) atomik doğrulayarak Gateway gecikmesini minimuma indirme. |
| **Split Query Optimization** | `ISeriesRepository.GetByIdWithSeasonAsync` | 1-N ilişkilerde kartezyen çarpımı önlemek için EF Core'un ilişkili tabloları ayrık SQL sorgularıyla çekmesi. |
| **Fail-Closed Gatekeeper** | `BaseJwtSocialAuthProvider.cs` | Güvenlik açısından kritik konfigürasyon hatalarında (canlıda imza atlama) sistemi güvenli tarafta tutarak yürütmeyi durdurma. |
| **Targeted Projections (Zero Over-fetching)** | `MoodRepository.GetExistingActiveIdsAsync`, `GenreRepository.GetExistingActiveIdsAsync` | Doğrulama adımlarında tüm entity ağacını çekmek yerine yalnızca indeksli ID listesini $O(1)$ `HashSet` kontrolü için çekme. |

---

## 4. Yapılan Geliştirme Nasıl Test Edilir?

### Test Senaryoları & Doğrulama Matrisi

1. **Birim & Entegrasyon Testleri (Terminal Çıktısı Kanıtı):**
   * `ShuffleSeries.Shared.Core.Tests`: 294 test geçti (0 hata).
   * `ShuffleSeries.Identity.Tests`: 144 test geçti (0 hata).
   * `ShuffleSeries.Catalog.Tests`: 376 test geçti (0 hata).
   * `ShuffleSeries.ArchitectureTests`: 63 test geçti (0 hata).
   * `ShuffleSeries.ApiGateway.IntegrationTests`: 7 test geçti (0 hata).
   * `ShuffleSeries.Catalog.IntegrationTests` (Testcontainers): 72 test geçti (0 hata).
   * `ShuffleSeries.Identity.IntegrationTests` (Testcontainers): 12 test geçti (0 hata).
   * **Toplam:** **968 test, %100 başarı, 0 hata.**

2. **Uçtan Uca Gateway Testleri (`python3 e2e_tests/run_all.py`):**
   * **API Gateway Edge & Security Suite:** 31 / 31 test geçti (Rate limiter, IP spoofing defense, Redis blacklist, CORS, correlation ID).
   * **Identity Service & Auth Suite:** 56 / 56 test geçti (Social login, ConvertGuest, PasswordChange, RefreshTokenRotation, Revocation, Deletion).
   * **Catalog Service & Media Suite:** 36 / 36 test geçti (Movies, Series, Episodes, Platforms, Moods, Genres, SQLi, ReDoS, IDOR pentestleri).
   * **Toplam:** **123 E2E senaryosu, %100 başarı.**

### Terminalde Çalıştırma Komutları

```bash
# 1. Çözümü derle
dotnet build

# 2. Tüm test paketlerini çalıştır
dotnet test

# 3. Docker konteynerlerini güncelle
docker compose -f docker-compose.yml -f docker-compose.apps.yml up -d --build catalog-api identity-api api-gateway

# 4. Gateway üzerinden tüm E2E testlerini yürüt
python3 e2e_tests/run_all.py
```
