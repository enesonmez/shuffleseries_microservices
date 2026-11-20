# Genel Refactoring: Proje Genelinde Performans, Güvenlik ve Mimari Yalınlık (Ponytail) Sertleştirmesi

## 1. Ne Yaptık?
Proje genelinde gerçekleştirilen kapsamlı denetim raporunda tespit edilen **17 adet performans, güvenlik ve mimari bulgu** over-engineering'den titizlikle kaçınılarak çözüldü:

1. **Çoklu Pod Outbox Kilidi & Kısmi İndeks (#1 & #6):**
   * [`OutboxBatchProcessor`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Infrastructure/Outbox/OutboxBatchProcessor.cs), PostgreSQL `FOR UPDATE SKIP LOCKED` satır kilitlerinin autocommit nedeniyle anında düşmesini engellemek için `BeginTransactionAsync` ve `CommitAsync` ile sarmalandı.
   * `PurgeProcessedMessagesAsync` çağrısı her 10 saniyede bir koşan tam tablo taraması yerine, `_purgeIntervalTicks` ve `Interlocked` ile en az 1 saatte bire kelepçelendi.
   * Hem Catalog hem Identity veritabanında `IX_OutboxMessages_Processed` (`WHERE "ProcessedOnUtc" IS NOT NULL`) kısmi indeksi tanımlandı.
2. **Pre-Account Takeover Savunması (#2 - Seçenek A):**
   * [`SocialLoginCommandHandler`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Identity/ShuffleSeries.Identity.Application/Features/Auth/Commands/SocialLogin/SocialLoginCommandHandler.cs), doğrulanmış bir sağlayıcıdan (`Google`/`Apple`, `email_verified == true`) gelen ilk bağlamada yerel `PasswordHash`'i sıfırlar, önceki tüm aktif refresh token'ları iptal eder ve Redis kara listesine alır.
3. **Ters Proxy IP Körlüğü & Rate-Limit Spoofing Savunması (#3):**
   * [`ForwardedHeadersExtensions`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Web/ForwardedHeaders/ForwardedHeadersExtensions.cs) ile `ForwardLimit = 1` sabitlendi ve Loopback + RFC 1918/4193 özel ağ CIDR'ları tanımlandı. Gateway ve Identity'deki kopya kodlar tekilleştirildi.
4. **Eşzamanlı Refresh Token Rotasyon Yarışı & `xmin` (#4):**
   * [`RefreshTokenConfiguration`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Identity/ShuffleSeries.Identity.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs) içine `xmin` row version eklendi; [`IdentityDbContext`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Identity/ShuffleSeries.Identity.Infrastructure/Persistence/IdentityDbContext.cs) seviyesinde `DbUpdateConcurrencyException ex when (ex.Entries.Any(e => e.Entity is RefreshToken))` hedefli exception filtresiyle yakalanarak yalnızca token çatışmasında `InvalidRefreshTokenException` fırlatılması sağlandı (aşırı genelleştirme önlendi).
5. **Sayfalama Integer Overflow Koruması (#5):**
   * [`PaginationRequest`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Application/Requests/PaginationRequest.cs) sınıfına `MaxPageNumber = 10_000` eklendi; property `init` seviyesinde zorunlu clamping uygulanarak negatif SQL `OFFSET` (500) hatası engellendi.
6. **Deterministik Scaffolding Şablon Düzeltmesi (#7):**
   * [`scaffold_service.py`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/scripts/scaffold_service.py) içindeki hardcoded veritabanı şifreleri kaldırıldı; `AddSharedApplication`, MediatR behaviors, EF Core interceptors, `validateTokenBlacklist: false` ve `IUnitOfWork` standartları şablona entegre edildi.
7. **KVKK/GDPR Hesap Silme & Log Maskeleme (#8):**
   * [`User.DeleteAccount`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Identity/ShuffleSeries.Identity.Domain/Entities/User.cs) PII alanlarını yerinde geri döndürülemez şekilde anonimleştirir (`deleted_{Id:N}@deleted.invalid`, IP sıfırlama); [`SensitiveDataDestructuringPolicy`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Web/Logging/SensitiveDataDestructuringPolicy.cs) e-posta ve IP adreslerini maskeler.
8. **Ağ İzolasyonu, Port Kısıtlamaları ve Gateway Rota Politikaları (#9):**
   * [`docker-compose.apps.yml`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/docker-compose.apps.yml) iç servis portları yalnızca `127.0.0.1` loopback'e bağlandı; YARP Gateway'de `shuffle-route` rotasına zorunlu auth politikası eklendi ve sağlık kontrolleri `/health/live` ile sınırlandırıldı.
9. **Güvenli Custom App Scheme Doğrulaması (#10):**
   * [`RuleBuilderExtensions`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Application/Validators/RuleBuilderExtensions.cs) içinde `intent://`, `http://`, `sms:`, `tel:` gibi protokoller yasaklanarak derin bağlantı güvenliği sağlandı.
10. **RBAC & Repository Değişiklikleri (#11):**
    * [`UserRepository`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Identity/ShuffleSeries.Identity.Infrastructure/Persistence/Repositories/UserRepository.cs) içinde süresi dolmuş token'ların ChangeTracker'a yüklenmesi engellendi.
11. **Misafir 30 Gün Kayar Oturum & Terk Edilen Misafir Temizliği (SRP Split - #12):**
    * PRD gereksinimine uygun olarak misafir oturumları 30 günlük kayar süreye hizalandı.
    * İki bağımsız Quartz işine ayrıldı (SRP): [`PurgeExpiredRefreshTokensJob`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Identity/ShuffleSeries.Identity.Infrastructure/BackgroundJobs/PurgeExpiredRefreshTokensJob.cs) süresi dolmuş token'ları; yeni oluşturulan [`PurgeAbandonedGuestAccountsJob`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Identity/ShuffleSeries.Identity.Infrastructure/BackgroundJobs/PurgeAbandonedGuestAccountsJob.cs) ise 60 gündür uğramayan terk edilmiş misafirleri ve cascade ilişkilerini (`UserRoles`, `RefreshTokens`, `UserLogins`, `UserPermissions`) veritabanı motoru seviyesinde temizler.
12. **Catalog Taksonomi Sınırı, Dil Normalizasyonu & Striped Lock (#13):**
    * Koleksiyonlara `<= 50` üst sınırı getirildi; [`LanguageContext`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Web/Localization/LanguageContext.cs) `?lang=en-US` gibi dilleri otomatik normalize etti; [`RedisCatalogCacheService`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Catalog/ShuffleSeries.Catalog.Infrastructure/Services/RedisCatalogCacheService.cs) sınırsız sözlük yerine sabit `SemaphoreSlim[64]` striped lock yapısına geçti.
13. **Ponytail Kod Silme & Refactoring (#14, #15, #16, #17):**
    * [`ProblemDetailsWriter`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Web/Helpers/ProblemDetailsWriter.cs) ile 3 farklı projedeki 100+ satır kopya kod silindi.
    * [`MediaTaxonomyApplier`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Catalog/ShuffleSeries.Catalog.Application/Common/Services/MediaTaxonomyApplier.cs) ile `CreateSeries` ve `CreateMovie` içindeki 106 satır kopya kod tekilleştirildi.
    * `HealthCheckExtensions` ve `RefreshToken` içindeki doğrudan `DateTime.UtcNow` kullanımları `TimeProvider` standardına bağlandı.
14. **Gateway Rota Temizliği (Unbuilt Service Route Purge):**
    * Henüz kodlanmamış servisler (`shuffle-service`, `search-service`) için Gateway `appsettings.json` ve `appsettings.Development.json` dosyalarında tanımlı rotalar ve cluster'lar temizlendi.
15. **Dinamik Vault Tabanlı Refresh Token Süreleri (Config-Driven Session Lifetimes):**
    * Misafir (30 gün) ve standart kullanıcı (14 gün) refresh token expire süreleri hardcoded değerlerden arındırıldı.
    * [`ITokenService`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Identity/ShuffleSeries.Identity.Application/Common/Interfaces/ITokenService.cs) ve [`JwtTokenService`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Identity/ShuffleSeries.Identity.Infrastructure/Services/JwtTokenService.cs) üzerinden `RefreshTokenLifetime` ve `GuestRefreshTokenLifetime` olarak dışa açıldı; tüm 6 auth komut işleyicisi güncellendi.
    * [`init-vault.sh`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/docker/vault/init-vault.sh) ve [`.env.example`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/.env.example) dosyalarına `JWT_REFRESH_TOKEN_DAYS` ve `JWT_GUEST_REFRESH_TOKEN_DAYS` anahtarları eklenerek tek gerçeklik kaynağından (SSOT) yönetilmesi sağlandı.
16. **ForwardedHeaders KnownProxies Vault & SSOT Entegrasyonu:**
    * [`ForwardedHeadersExtensions`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Web/ForwardedHeaders/ForwardedHeadersExtensions.cs) hem konfigürasyon dizi (`:0`, `:1`) hem de virgülle ayrılmış string formatını destekleyecek şekilde esnetildi.
    * [`init-vault.sh`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/docker/vault/init-vault.sh) içinde `shuffleseries/shared` altına `ForwardedHeaders:KnownProxies:0=${FORWARDED_HEADERS_KNOWN_PROXIES:-127.0.0.1}` eklendi; [`.env.example`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/.env.example) ile tek gerçeklik kaynağı sağlandı.
    * [`ForwardedHeadersExtensionsTests.cs`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Tests/Web/ForwardedHeaders/ForwardedHeadersExtensionsTests.cs) birim testleri ile korundu.

47: 17. **EF Core Model Değişiklikleri & Snapshot Senkronizasyonu (#1, #4, #6):**
48:     * `Identity.Infrastructure` için `AddRefreshTokenRowVersionAndOutboxProcessedIndex` ve `Catalog.Infrastructure` için `AddCatalogOutboxProcessedIndex` migration'ları üretildi. Model snapshot'ları `xmin` ve `IX_OutboxMessages_Processed` ile %100 senkronize edildi (`has-pending-model-changes: 0`).
49: 18. **Identity RBAC MemoryCache & UserRepository İzolasyonu (#11):**
50:     * [`PermissionResolver`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Identity/ShuffleSeries.Identity.Infrastructure/Services/PermissionResolver.cs) değişmez rol-izin haritasını 1 saat TTL ile `IMemoryCache` üzerinde önbelleğe aldı.
51:     * [`UserRepository`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Identity/ShuffleSeries.Identity.Infrastructure/Persistence/Repositories/UserRepository.cs) normal şifre girişlerinde `GetByEmailAsync` üzerinden gereksiz `UserLogins` koleksiyonunu yüklemeyi bıraktı; sosyal hesap bağlama için ayrık `GetByEmailWithLoginsAsync` metodu oluşturuldu.
52: 19. **Katalog Endpoint'lerinde Dil SSOT Standardı (`LanguageContext.CurrentLanguage` - #13.3):**
53:     * [`SeriesEndpoints`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Catalog/ShuffleSeries.Catalog.Api/Endpoints/SeriesEndpoints.cs), [`MovieEndpoints`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Catalog/ShuffleSeries.Catalog.Api/Endpoints/MovieEndpoints.cs), [`MoodEndpoints`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Catalog/ShuffleSeries.Catalog.Api/Endpoints/MoodEndpoints.cs), [`GenreEndpoints`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Catalog/ShuffleSeries.Catalog.Api/Endpoints/GenreEndpoints.cs), [`EpisodeEndpoints`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Catalog/ShuffleSeries.Catalog.Api/Endpoints/EpisodeEndpoints.cs) ve [`AdminMediaEndpoints`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Catalog/ShuffleSeries.Catalog.Api/Endpoints/AdminMediaEndpoints.cs) üzerinden redundant `[FromQuery] string? lang` parametresi tamamen kaldırıldı. Tüm sorgular hem `?lang=` hem `Accept-Language` başlığını sanitize ve valide eden `languageContext.CurrentLanguage`'e bağlandı.
54: 20. **Admin Media İstenen Dil Sıralaması & Projeksiyon (#13.2):**
55:     * [`MediaRepository.GetAdminMediaPagedListAsync`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Catalog/ShuffleSeries.Catalog.Infrastructure/Persistence/Repositories/MediaRepository.cs) `targetLanguage` parametresi alarak `sortBy=title` durumunda SQL `COALESCE` ile istenen dildeki başlığı önceliklendirecek şekilde güncellendi.
56: 21. **Microsoft MIT Lisanslı HTTP Dayanıklılık Standardı (`Microsoft.Extensions.Http.Resilience` - #13.5):**
57:     * Polly yerine Microsoft'un resmi BCL standardı olan `Microsoft.Extensions.Http.Resilience` (10.11.0) entegre edildi. `"OidcJwks"` istemcisine `.AddStandardResilienceHandler()` (rate limiting, timeout, 3x exponential retry, circuit breaker) uygulandı; [`BaseJwtSocialAuthProvider`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Identity/ShuffleSeries.Identity.Infrastructure/Services/Social/BaseJwtSocialAuthProvider.cs) JWKS network hataları açıkça `Warning` seviyesinde loglandı.
58: 22. **Mükerrer `OutboxMessageConfiguration` DRY Konsolidasyonu (#16.2):**
59:     * Catalog ve Identity içindeki birebir aynı konfigürasyon sınıfları silinerek [`OutboxMessageConfiguration`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Infrastructure/Persistence/Configurations/OutboxMessageConfiguration.cs) dosyasına taşındı ve DbContext `OnModelCreating` içerisinde tek satırda kaydedildi.
60: 23. **ReDoS Korumalı Merkezi IMDb ID Validatörü (`MustBeImdbId` - #13.1):**
61:     * [`RuleBuilderExtensions`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Application/Validators/RuleBuilderExtensions.cs) içinde .NET Source Generator destekli `[GeneratedRegex(@"^tt\d{7,10}$", RegexOptions.None, matchTimeoutMilliseconds: 250)]` tanımlandı ve `CreateSeriesCommandValidator` ile `CreateMovieCommandValidator` sınıflarında kullanıldı.
62: 24. **ProblemDetails Yazıcısının Bütünleşik Kullanımı (#14):**
63:     * [`GlobalExceptionHandler`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Web/Middlewares/GlobalExceptionHandler.cs) ve [`RateLimitingExtensions`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Web/RateLimiting/RateLimitingExtensions.cs) doğrudan [`ProblemDetailsWriter.WriteAsync`](file:///Users/enesonmez/RiderProjects/ShuffleSeries.Microservices/ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Web/Helpers/ProblemDetailsWriter.cs) standardına geçirildi; mükerrer RFC 7807 alan üretimi tamamen ortadan kaldırıldı.
64: 
65: ## 2. Neden Yaptık?
66: * **Zero Loop I/O ve Ölçeklenebilirlik:** Outbox temizliği sırasında her 10 saniyede bir tam tablo taraması yapılması veritabanı CPU'sunu tüketiyordu; kısmi indeks ve saatlik kısıtlama bu yükü sıfırladı.
67: * **Security by Design:** Doğrulanmamış hesaplara sosyal giriş yapıldığında şifrenin açık kalması hesap ele geçirme açığına (Pre-Account Takeover) yol açıyordu; yerinde şifre temizliği ve token iptali bu sınırı kapattı.
68: * **Lisans Güvenliği:** Polly'nin v8 ile değişen lisans modeline karşılık, Microsoft'un resmi ve MIT lisanslı `Microsoft.Extensions.Http.Resilience` kütüphanesine geçilerek hukuki ve operasyonel riskler bertaraf edildi.
69: * **Ponytail Sadeliği:** Yeni kütüphane veya katman eklemek yerine, mevcut kopya kodlar silinerek ve BCL özellikleri (`TimeProvider`, `Interlocked`, striped array, `GeneratedRegex`) kullanılarak sistem yalınlaştırıldı.
70: 
71: ## 3. Kullanılan Tasarım Desenleri
72: * **Transactional Outbox & Row-Level Locking:** PostgreSQL `FOR UPDATE SKIP LOCKED` + `BeginTransactionAsync`.
73: * **Striped Lock Pattern:** Sabit boyutlu `SemaphoreSlim[64]` dizisi ile $O(1)$ bellek tahsissiz Thundering Herd savunması.
74: * **Standard HTTP Resilience Pipeline:** Rate limiting, retry, timeout ve circuit breaker'ı tek hatta toplayan pipeline deseni.
75: * **Fail-Closed & Defense-in-Depth:** Ters proxy ardında `ForwardLimit = 1` ve `KnownIPNetworks` ile IP izolasyonu.
76: * **Irreversible Anonymization (Crypto-Shredding):** GDPR/KVKK uyumlu yerinde kimliksizleştirme.
77: 
78: ## 4. Nasıl Test Edilir?
79: ```bash
80: # 1. Derleme
81: dotnet build
82: 
83: # 2. Tüm Unit ve Mimari Testler (934 test - 0 hata, 0 başarısızlık)
84: dotnet test --filter "FullyQualifiedName!~Catalog.IntegrationTests&FullyQualifiedName!~Identity.IntegrationTests"
85: 
86: # 3. Biçimlendirme Doğrulaması
87: dotnet format --verify-no-changes
88: ```
