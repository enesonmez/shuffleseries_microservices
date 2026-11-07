# Görev Dokümanı: Mikroservisler Arası Derin Kod, Katman ve Yönetişim Simetrisi Standardizasyonu
*(Refactor: Deep Cross-Service Layer, Code & Governance Symmetry Alignment)*

---

## 1. Ne Yaptık?

Bu kapsamlı mimari standardizasyon çalışmasında; `ShuffleSeries.Catalog` ve `ShuffleSeries.Identity` mikroservislerinin tüm Onion katmanları (`Domain`, `Application`, `Infrastructure`, `Api`) ve test projeleri (`Tests`, `IntegrationTests`) satır ve dosya bazında hizalanmış, alan-özgü (domain-specific) meşru farklılıklar korunurken tüm yapısal asimetriler kalıcı olarak giderilmiştir:

1. **Faz 1 — `.csproj` ve Saf `Infrastructure` Katmanı İzolasyonu:**
   - [PasswordHasher.cs](../../ShuffleSeries.Identity/ShuffleSeries.Identity.Infrastructure/Services/PasswordHasher.cs) içerisindeki `Microsoft.AspNetCore.Cryptography.KeyDerivation` bağımlılığı kaldırılarak doğrudan **.NET 10 BCL `Rfc2898DeriveBytes.Pbkdf2` (`HashAlgorithmName.SHA512`)** kriptografik motoruna geçildi.
   - Atıl kalan `IdentityDbContextFactory.cs` silindi; [ShuffleSeries.Identity.Infrastructure.csproj](../../ShuffleSeries.Identity/ShuffleSeries.Identity.Infrastructure/ShuffleSeries.Identity.Infrastructure.csproj) içerisinden `<FrameworkReference Include="Microsoft.AspNetCore.App" />` ve `Microsoft.EntityFrameworkCore.Design` tamamen temizlendi.
   - `IHttpClientFactory` desteğinin web çatısı olmadan çalışabilmesi için [ShuffleSeries.Shared.Core.Infrastructure.csproj](../../ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Infrastructure/ShuffleSeries.Shared.Core.Infrastructure.csproj) içerisine `Microsoft.Extensions.Http` eklendi.
   - `Catalog.Domain` ve `Identity.Domain` projelerinde `<InternalsVisibleTo>` tanımları ve tüm katmanlardaki global `<Using>` yönergeleri birebir eşitlendi.

2. **Faz 2 — `Infrastructure` Katmanı Kod ve Performans Hizalaması:**
   - [GenreRepository.cs](../../ShuffleSeries.Catalog/ShuffleSeries.Catalog.Infrastructure/Persistence/Repositories/GenreRepository.cs), [MoodRepository.cs](../../ShuffleSeries.Catalog/ShuffleSeries.Catalog.Infrastructure/Persistence/Repositories/MoodRepository.cs) ve [PlatformRepository.cs](../../ShuffleSeries.Catalog/ShuffleSeries.Catalog.Infrastructure/Persistence/Repositories/PlatformRepository.cs) `Update(...)` metotlarına `if (_context.Entry(entity).State == EntityState.Detached)` koruması eklendi.
   - `Catalog` [ProcessOutboxMessagesJob.cs](../../ShuffleSeries.Catalog/ShuffleSeries.Catalog.Infrastructure/BackgroundJobs/ProcessOutboxMessagesJob.cs) sınıfına `TimeProvider` ve yapılandırılmış `ILogger` (`IsEnabled` guard ile) enjekte edilerek `Identity` ile %100 simetrik hale getirildi.
   - `Identity` [OutboxMessageConfiguration.cs](../../ShuffleSeries.Identity/ShuffleSeries.Identity.Infrastructure/Persistence/Configurations/OutboxMessageConfiguration.cs) kısmi indeksi `IX_OutboxMessages_Pending` (`OccurredOnUtc` üzerinde `WHERE "ProcessedOnUtc" IS NULL AND "RetryCount" < 3`) olarak optimize edildi ve EF Core migration'ı üretildi.
   - `Identity` MassTransit RabbitMQ yapılandırmasına OOM koruması için `configurator.PrefetchCount = 16;` eklendi.
   - `Catalog` altındaki 16 EF Core konfigürasyon sınıfının tamamı `internal sealed class` yapıldı; `ExternalAuthOptions.cs` içerisindeki sınıflar **1-Class-1-File** kuralı uyarınca [GoogleAuthOptions.cs](../../ShuffleSeries.Identity/ShuffleSeries.Identity.Infrastructure/Configuration/GoogleAuthOptions.cs) ve [AppleAuthOptions.cs](../../ShuffleSeries.Identity/ShuffleSeries.Identity.Infrastructure/Configuration/AppleAuthOptions.cs) dosyalarına ayrıldı.

3. **Faz 3 — `Application` Katmanı Encapsulation ve 1-Class-1-File Hizalaması:**
   - `Catalog.Application` içerisindeki 43 CQRS Handler sınıfının tamamı `public sealed class` yerine `internal sealed class` yapılarak uygulama katmanı dışına sızmaları kapatıldı.
   - `Identity.Application` içerisindeki 10 CQRS Command/Query kaydı ve `Catalog.Application` içerisindeki [DeleteSeriesCommand.cs](../../ShuffleSeries.Catalog/ShuffleSeries.Catalog.Application/Features/Series/Commands/DeleteSeries/DeleteSeriesCommand.cs) `public sealed record` olarak mühürlendi.
   - Endpoint yanıt modelleri olan [AuthResponse.cs](../../ShuffleSeries.Identity/ShuffleSeries.Identity.Application/Features/Auth/Responses/AuthResponse.cs) ve [CurrentUserResponse.cs](../../ShuffleSeries.Identity/ShuffleSeries.Identity.Application/Features/Auth/Responses/CurrentUserResponse.cs), `Common/Models/` altından dikey dilim klasörü olan `Features/Auth/Responses/` altına taşındı.
   - `Catalog.Application` altında birden fazla DTO barındıran 7 birleşik (bundle) dosya parçalanarak her record kendi tekil `.cs` dosyasına kavuşturuldu.

4. **Faz 4 — `Api` Katmanı Yapılandırma ve Container Hizalaması:**
   - [ShuffleSeries.Catalog.Api/Dockerfile](../../ShuffleSeries.Catalog/ShuffleSeries.Catalog.Api/Dockerfile) içerisine eksik `ShuffleSeries.Shared.Core.Infrastructure.csproj` katman önbellekleme (`COPY`) satırı eklendi.
   - `Catalog.Api/appsettings.Development.json` ve `Identity.Api/Properties/launchSettings.json` (`https` profili dahil) dosyaları iki servis arasında standartlaştırıldı.

5. **Faz 5 — Birim ve Entegrasyon Testlerinde 1:1 SUT Aynalama & Seeder SSOT:**
   - `ShuffleSeries.Catalog.Tests/Application/Features/` altındaki 40 düz test dosyası, test ettikleri SUT (System Under Test) klasör ve namespace hiyerarşisine birebir taşındı; 3 çoklu handler test dosyası 16 bağımsız test dosyasına bölündü (**63 feature test dosyası, 0 SUT uyumsuzluğu**).
   - [IdentityIntegrationTestBase.cs](../../ShuffleSeries.Identity/ShuffleSeries.Identity.IntegrationTests/Infrastructure/IdentityIntegrationTestBase.cs) ve [IdentityApiFactory.cs](../../ShuffleSeries.Identity/ShuffleSeries.Identity.IntegrationTests/Infrastructure/IdentityApiFactory.cs) içerisindeki 4 izinlik kısmi manuel tohumlama kaldırılarak üretim ortamıyla aynı 30 izinlik [IdentityDbSeeder.cs](../../ShuffleSeries.Identity/ShuffleSeries.Identity.Infrastructure/Persistence/IdentityDbSeeder.cs) tek gerçeklik kaynağı (SSOT) olarak bağlandı.

6. **Faz 6 — Yürütülebilir Mimari Yönetişim ve Scaffolding Kilitleri:**
   - [MicroserviceArchitectureTestBase.cs](../../ShuffleSeries.ArchitectureTests/Common/MicroserviceArchitectureTestBase.cs) sınıfına `Infrastructure_ShouldNotHaveDependencyOn_AspNetCore`, `Handlers_ShouldBeInternal`, `CommandsAndQueries_ShouldBeSealed` ve `EntityTypeConfigurations_..._And_BeInternalSealed` kuralları eklendi.
   - [scripts/scaffold_service.py](../../scripts/scaffold_service.py) ve [.agents/rules/architecture-standard.md](../../.agents/rules/architecture-standard.md) yeni kurallarla %100 uyumlu olacak şekilde güncellendi.

---

## 2. Neden Yaptık? (Mimari Kararlar ve Teknik Nedenler)

* **Neden `Infrastructure` Katmanından `Microsoft.AspNetCore.App` Kaldırıldı?**
  Onion Architecture'da `Infrastructure` katmanı veri erişimi, mesajlaşma ve dış servis adaptörlerinden sorumludur; HTTP pipeline'ı, `HttpContext` veya web sunucusu tiplerini bilmemelidir. `FrameworkReference Include="Microsoft.AspNetCore.App"` referansı açık bırakıldığında geliştiricilerin yanlışlıkla `Infrastructure` içine `HttpContext` veya web middleware bağımlılıkları sızdırması derleyici tarafından engellenemez. Saf .NET 10 BCL `Rfc2898DeriveBytes.Pbkdf2` aynı NIST SP 800-132 PBKDF2-HMAC-SHA512 algoritmasını sıfır web bağımlılığıyla çalıştırır ve geri dönük tüm parola özetleriyle (hash) bit-seviyesinde %100 uyumludur.
* **Neden CQRS Handler ve EF Core Konfigürasyon Sınıfları `internal sealed` Olmalı?**
   Sunum (`Api`) katmanı Application katmanıyla yalnızca `ISender` (`MediatR`) ve Command/Query sözleşmeleri üzerinden konuşmalıdır. Handler'ların `public` olması, endpoint'lerin MediatR pipeline davranışlarını (`ValidationBehavior`, `LoggingBehavior`, `PerformanceBehavior`) bypass ederek handler'ı doğrudan DI üzerinden çağırmasına kapı aralar. `internal sealed` erişim belirleyicisi hem kapsüllemeyi (encapsulation) korur hem de JIT derleyicisinin sanal tablo (devirtualization) optimizasyonunu etkinleştirir.
* **Neden Repository `Update(...)` Metodunda `EntityState.Detached` Kontrolü Zorunlu?**
  EF Core'da ChangeTracker tarafından zaten izlenen (`Tracked`) bir aggregate'e yeni bir alt varlık (`base(Guid.Empty)` ile üretilmiş `GenreTranslation` veya `RefreshToken`) eklendiğinde, koşulsuz `_context.Set<T>().Update(entity)` çağrısı tüm nesne grafiğini zorla `Modified` durumuna geçirir. Bu durum, yeni eklenen çocuk kaydın `INSERT` yerine `UPDATE` edilmeye çalışılmasına (`DbUpdateConcurrencyException`) ve değişmemiş kolonlar için bile gereksiz `UPDATE` SQL / WAL üretilmesine yol açar.
* **Neden `Identity` Entegrasyon Testlerinde `IdentityDbSeeder` SSOT Kullanıldı?**
  Test ortamında ayrı ve eksik bir manuel seed metodu tutmak ("Test-Production Configuration Drift"), yeni eklenen izinlerin (`catalog:associations:manage` vb.) üretimde çalışırken entegrasyon testlerinde görünmemesine veya tam tersine yol açar. Gerçek `IdentityDbSeeder.SeedAsync` çağrısı idempotent yapısıyla hem başlangıçta hem de `Respawn` temizliği sonrasında milisaniyeler içinde gerçek üretim yetki matrisini kurar.

---

## 3. Hangi Pattern'leri Kullandık?

1. **Architectural Fitness Functions (`NetArchTest.Rules`):** Mimari kuralların pasif dokümanlar yerine her derleme ve CI koşusunda otomatik doğrulanan birim testlerine (`MicroserviceArchitectureTestBase`) dönüştürülmesi.
2. **Pure Layer Isolation (Onion Architecture):** Alt ve yan katmanların (`Domain`, `Application`, `Infrastructure`) üst çerçeve (`Microsoft.AspNetCore.*`) sızıntılarından tamamen arındırılması.
3. **Vertical Slice Encapsulation & 1-Class-1-File:** Her CQRS dikey diliminin kendi yanıt modellerini (`Features/{Feature}/Responses/`) barındırması, handler'ların `internal sealed` ile gizlenmesi ve her C# tipinin kendi dosyasında yaşaması.
4. **SUT (System Under Test) Folder Mirroring:** Birim test dizin ve namespace ağacının, test edilen üretim kodunun dizin ağacını $1:1$ yansıtması.
5. **Filtered Partial Outbox Index (`IX_OutboxMessages_Pending`):** PostgreSQL kısmi B-Tree indeksi ile milyonlarca işlenmiş Outbox satırını indeks dışında bırakarak arka plan kuyruk okuyucusunun sabit $O(\log N_{\text{pending}})$ hızında çalışması.

---

## 4. Yapılan Geliştirme Nasıl Test Edilir?

### 4.1. Doğrulama Komutları
```bash
# 1. Tüm çözümün sıfır uyarı ve sıfır hata ile derlenmesi
dotnet build ShuffleSeries.sln

# 2. Evrensel Mimari Fitness Testlerinin (59 Kural) koşturulması
dotnet test ShuffleSeries.ArchitectureTests/ShuffleSeries.ArchitectureTests.csproj

# 3. Birim + Mimari + Gerçek Testcontainers (PostgreSQL, Redis, RabbitMQ) Entegrasyon Testleri
dotnet test ShuffleSeries.sln

# 4. Scaffolding motorunun yeni şablonlarla dry-run doğrulaması
python3 scripts/scaffold_service.py ShuffleEngine --port 5012 --dry-run

# 5. YARP API Gateway üzerinden uçtan uca (E2E) test otomasyonu
python3 e2e_tests/run_all.py
```

### 4.2. Kritik Test Senaryoları (Test Cases)
1. **RFC 2898 PBKDF2-HMAC-SHA512 Geriye Dönük Vektör Uyumluluğu:**
   - `PasswordHasherTests.VerifyPassword_WithKnownPbkdf2HmacSha512Vector_ShouldReturnTrue`: Bilinen sabit salt ve 100.000 iterasyonlu SHA-512 vektörünün (`AQIDBAUGBwgJCgsMDQ4PEA==:ti06sjaXeqguZRogVkA/CtarTvJNHrjB3YWOWwR+1C0=`) yeni BCL `Rfc2898DeriveBytes.Pbkdf2` implementasyonu tarafından doğrulandığını garanti eder.
2. **Infrastructure ASP.NET Core İzolasyon Kuralı:**
   - `Infrastructure_ShouldNotHaveDependencyOn_AspNetCore`: Hem `Catalog.Infrastructure` hem de `Identity.Infrastructure` derlemelerinin hiçbir `Microsoft.AspNetCore.*` namespace'ine bağımlı olmadığını doğrular.
3. **Internal Sealed Handler & Sealed Command/Query Kuralı:**
   - `Handlers_ShouldBeInternal`, `Handlers_ShouldBeSealed`, `CommandsAndQueries_ShouldBeSealed`: Tüm mikroservislerdeki handler ve CQRS mesajlarının erişim ve kalıtım sınırlarını denetler.
4. **Tracked Aggregate Üzerinde Çeviri Ekleme/Güncelleme/Silme:**
   - `PlatformMoodGenreEndpointsIntegrationTests.AddAndRemoveGenreTranslation_ShouldPersist_AndInvalidateCache`: İzlenen `Genre` aggregate'i üzerinde hem yeni çeviri ekleme hem mevcut çeviriyi güncelleme (`Detached` guard sayesinde concurrency hatası almadan) hem de Redis önbellek invalidasyonunu gerçek PostgreSQL ve Redis üzerinde doğrular.
