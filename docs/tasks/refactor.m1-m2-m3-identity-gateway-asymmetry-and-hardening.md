# Refactoring & Sıkılaştırma: Identity ve API Gateway Asimetri Düzeltmeleri, Performans ve Mimari Sertleştirme

Bu doküman, `ShuffleSeries.Catalog` servisinde kurulan Clean Architecture, Ponytail sadeliği, Performans Muhafızı ve NetArchTest standartlarının `ShuffleSeries.Identity` ve `ShuffleSeries.ApiGateway` servislerine aktarılması sürecini ve alınan teknik kararları özetler.

---

## 1. Ne Yaptık? (Genel Özet)

1. **Identity Repositories ChangeTracker İzolasyonu:**
   * `UserRepository`, `RoleRepository` ve `PermissionRepository` sınıflarında `Update` metotlarına `if (_context.Entry(entity).State == EntityState.Detached)` kontrolü eklendi.
   * `UserRepository.GetByIdWithDetailsAsync` sorgusuna `.AsNoTracking()` eklendi; `/api/auth/me` profil okumalarında ChangeTracker snapshot yükü sıfırlandı.
2. **NetArchTest Mimari Kurallarının Eşitlenmesi:**
   * `CreateGuestSessionCommand` için `CreateGuestSessionCommandValidator` yazılarak %100 komut validasyon standardı sağlandı.
   * `IdentityArchitectureTests` içerisine `Catalog`'da bulunan 5 kritik mimari test kuralı entegre edildi:
     - `RepositoryMethods_ShouldHave_CancellationTokenAsLastParameter`
     - `Repositories_InInfrastructure_ShouldHaveNameEndingWith_Repository_And_BeInternalSealed`
     - `EntityTypeConfigurations_ShouldHaveNameEndingWith_Configuration_And_BeInConfigurationsNamespace`
     - `ChildEntities_ShouldNotInheritFrom_AggregateRoot`
     - `Commands_ShouldHave_CorrespondingValidator`
3. **API Gateway RFC 7807 Global Hata ve Proxy Kesinti Yönetimi:**
   * `ShuffleSeries.ApiGateway.Api` projesine `AddSharedExceptionHandling()` ve `UseSharedExceptionHandling()` middleware'leri eklendi.
   * YARP pipeline'ı içerisine downstream mikroservisler (Catalog/Identity) ulaşılamaz veya zaman aşımına uğradığında (`NoAvailableDestinations`, `RequestTimedOut`) boş gövde yerine RFC 7807 `ProblemDetails` (`"code": "DOWNSTREAM_SERVICE_UNAVAILABLE"`, `traceId`, `correlationId`) dönen hata transformatörü entegre edildi.
4. **C# Gateway Integration Test Zenginleştirmesi:**
   * `GatewayIntegrationTests.cs` içerisindeki kırılgan `await Task.Delay(1100)` kaldırıldı; izole `X-Forwarded-For` IP başlıkları ile deterministik rate limiting sağlandı.
   * Correlation ID (`X-Correlation-ID`) üretimi ve korunumu, Liveness/Readiness probe doğrulamaları, Swagger UI erişimi ve downstream ulaşılamazlık durumlarında ProblemDetails dönüşü testleri eklendi (toplam 7 test).
5. **Catalog & Identity %100 CQRS Dosya Simetrisi (Single Responsibility):**
   * `ChangePassword`, `ConvertGuest`, `CreateGuestSession`, `DeleteAccount`, `RevokeToken`, `SocialLogin` komutları ve `GetCurrentUser` sorgusu; `Login`, `Register`, `RefreshToken` ve `Catalog`'daki altın standart ile %100 simetrik olacak şekilde `Command.cs`, `CommandHandler.cs` ve `CommandValidator.cs` olarak bağımsız dosyalara ayrıştırıldı. Dosya düzeyinde Single Responsibility Principle (SRP) sağlandı.
6. **Katman ve Klasör Asimetrisi Standardizasyonu (1 Class = 1 File & SUT Mirroring):**
   * **Infrastructure Configurations:** `Identity.Infrastructure` içerisindeki çok sınıflı `JunctionConfigurations.cs` ve `AuthTokenConfigurations.cs` dosyaları silinerek her configuration sınıfı (`UserRoleConfiguration.cs`, `RolePermissionConfiguration.cs`, `UserPermissionConfiguration.cs`, `UserLoginConfiguration.cs`, `RefreshTokenConfiguration.cs`) kendi bağımsız dosyasına ayrıştırıldı.
   * **Application Common/ Hiyerarşisi:** `Identity.Application` altındaki düzensiz `Interfaces/` ve `Models/` dizinleri `Catalog.Application` ile uyumlu hale getirilerek `Common/Interfaces/` ve `Common/Models/` dizinlerine taşındı; namespace'ler standartlaştırıldı.
   * **Tests SUT Folder Mirroring:** `Identity.Tests` içerisindeki düzensiz dosya dizilimi giderildi. Test dizin yapısı, test edilen sınıfların gerçek konumları ile birebir eşleştirildi (`Application/Features/Auth/Commands/...`, `Domain/Entities/UserTests.cs`, `Domain/Exceptions/IdentityDomainExceptionTests.cs`).
   * **Domain Exceptions 1-to-1 Splitting:** `Catalog.Domain/Exceptions/CatalogExceptions.cs` içerisine yığılmış 19 adet exception sınıfı, `Identity.Domain` standardına uygun şekilde bağımsız 19 dosyaya (`MediaNotFoundException.cs`, `MovieNotFoundException.cs` vb.) ayrıştırıldı.
   * **API Requests Modülerleşmesi:** `Identity.Api/Contracts/Requests/AuthRequests.cs`, Catalog ile simetrik olacak şekilde `Contracts/Requests/Auth/AuthRequests.cs` dizinine ve namespace'ine taşındı.
7. **Nihai Asimetri Giderme ve Kalite Paritesi (Final Parity):**
   * **Catalog Domain Events 1-to-1:** `Catalog.Domain/Events/` içerisindeki 5 adet bundle dosya (`EpisodeEvents.cs`, `MediaEvents.cs` vb.) silinerek 15 event bağımsız strongly-typed dosyalara (`EpisodeCreatedDomainEvent.cs` vb.) dönüştürüldü.
   * **SUT Klasör Aynalaması:** `Catalog.Tests` içerisindeki Series testleri `Application/Features/Series/Commands/` ve `Queries/` altına taşındı. `Identity.Tests` içerisindeki altyapı servis testleri (`PasswordHasherTests.cs`, `PermissionResolverTests.cs`, `SocialAuthProviderTests.cs`) `Infrastructure/Services/` altına taşındı.
   * **NetArchTest Kural Paritesi:** `IdentityArchitectureTests` içerisine `Commands_ShouldHaveNameEndingWith_Command`, `Queries_ShouldHaveNameEndingWith_Query` ve `Entities_ShouldInheritFrom_BaseEntityOrAggregateRoot` kuralları eklendi.
   * **Identity DI & Validator Testleri:** `Identity.Tests` içerisine `Application/DependencyInjectionTests.cs`, `Infrastructure/DependencyInjectionTests.cs` ve 9 adet izole `*CommandValidatorTests.cs` dosyası eklendi (toplam test sayısı 88'den 132'ye yükseldi).
   * **Redis Health Check Simetrisi:** `Catalog.Api/Program.cs`'deki `AddSharedRedisHealthCheck()` çağrısı `Catalog.Infrastructure/DependencyInjection.cs`'e taşınarak iki mikroservisin altyapı kohezyonu eşitlendi.
   * **API Geliştirme Profili:** `Identity.Api/Properties/launchSettings.json` oluşturuldu.

---

## 2. Neden Yaptık? (Mimari Kararların Arkasındaki Nedenler)

* **Neden ChangeTracker Detached Guard?**
  EF Core'da halihazırda takip edilen (`Tracked`) bir varlığa körü körüne `_context.Update(entity)` çağrıldığında, EF Core tüm property'leri zorla `EntityState.Modified` yapar. Bu durum veritabanına gereksiz `UPDATE` SQL ifadeleri, aşırı WAL (Write-Ahead Log) yazımı ve satır kilidi (row-level lock contention) olarak yansır. Varlık sadece `Detached` durumdaysa update çağırmak veritabanı I/O maliyetini düşürür.
* **Neden Salt Okumalarda `.AsNoTracking()`?**
  Kullanıcı profil sorgusu gibi CQRS Query işlemlerinde varlık üzerinde değişiklik yapılmaz. ChangeTracker'ın snapshot almasını engellemek Garbage Collector (Gen0) baskısını ve RAM tahsisini minimize eder.
* **Neden Gateway Seviyesinde Proxy Error Transform?**
  İç servislerden biri kapalı veya yeniden başlatılıyor olduğunda YARP varsayılan olarak boş gövdeli 502/503/504 döner. Mobil veya web istemcilerinin her koşulda öngörülebilir RFC 7807 ProblemDetails yanıtı alması istemci tarafındaki hata yönetimini sağlamlaştırır.
* **Neden NetArchTest Bütünlüğü?**
  Büyük mikroservis projelerinde bir serviste uygulanan mimari disiplinin diğer servislerde uygulanmaması mimari çürümeye (architectural erosion) yol açar. Kuralların NetArchTest ile otomatik denetlenmesi mimarinin korunmasını garanti eder.

---

## 3. Hangi Pattern'leri Kullandık?

1. **Onion Architecture & DDD Encapsulation:** Domain modellerinin ve child entity'lerin encapsulation sınırlarının korunması.
2. **CQRS & Mediator Pattern:** Read ve Write operasyonlarının tam ayrımı; salt okumalarda `.AsNoTracking()` kullanımı.
3. **Fitness Functions (NetArchTest):** Mimari kuralların birim test olarak otomatik icra edilmesi (Evolutionary Architecture).
4. **Gateway Offloading & Edge Hardening:** Ters proxy üzerinde RFC 7807 hata standardizasyonu ve deterministik IP bölümlendirmeli hız sınırlaması.
5. **Single Responsibility & Vertical Slice Symmetry:** Her CQRS bileşeni (Request DTO, Handler, Validator) bağımsız dosyalarda tutularak IDE navigasyonu, kod okunabilirliği ve mikroservisler arası standartlaştırılmış mimari simetri sağlandı.

---

## 4. Nasıl Test Edilir? (Test Senaryoları)

### Terminal Üzerinden Test
```bash
# 1. Mimari testleri koştur (5 yeni kural dahil)
dotnet test ShuffleSeries.ArchitectureTests --filter FullyQualifiedName~IdentityArchitectureTests

# 2. Gateway entegrasyon testlerini koştur
dotnet test ShuffleSeries.ApiGateway/ShuffleSeries.ApiGateway.IntegrationTests

# 3. Tüm çözüm testlerini koştur (930 test, %100 Başarılı)
dotnet test

# 4. Kod stil doğrulaması
dotnet format --verify-no-changes
```

### Örnek Test Case'leri (Gateway & Identity):
1. **CorrelationId Generation:** İstemci `X-Correlation-ID` göndermediğinde Gateway geçerli bir GUID üretip yanıtta döner.
2. **CorrelationId Preservation:** İstemci `X-Correlation-ID: custom-123` gönderdiğinde Gateway bu değeri bozmadan yanıtta yansıtır.
3. **Downstream Unreachable:** Ulaşılamayan bir servise istek atıldığında Gateway RFC 7807 `application/problem+json` ve `"code": "DOWNSTREAM_SERVICE_UNAVAILABLE"` döner.
4. **Rate Limiting Strict Window:** Aynı IP'den 1 saniye içinde 5 istekten fazlası atıldığında 6. istek anında HTTP 429 Too Many Requests alır.
5. **Deterministic IP Isolation:** Farklı `X-Forwarded-For` IP'lerinden gelen istekler birbirinin hız sınırını etkilemez.
