# Görev Dokümanı: Gerçek Altyapı Entegrasyon Testleri ve CI Kararlılığı
*(Task: Real Infrastructure Integration Tests with Testcontainers & CI Stability)*

---

## 1. Ne Yaptık?
Bu görev kapsamında; GitHub Actions CI hattında `dotnet test` adımının zaman zaman belirsiz sürelerle kilitlenmesine (hang/freeze) yol açan tüm kök nedenler tespit edilerek kalıcı olarak çözüldü ve entegrasyon testi altyapısı **%100 gerçek altyapı bağımlılıkları (Real Infrastructure)** ile modernize edildi:

1. **Tam Altyapı Testcontainers Mimarisi:**
   - Catalog ve Identity entegrasyon testlerine `Testcontainers.Redis` (v4.3.0) ve `Testcontainers.RabbitMq` (v4.3.0) dahil edildi.
   - [CatalogApiFactory.cs](../../ShuffleSeries.Catalog/ShuffleSeries.Catalog.IntegrationTests/Infrastructure/CatalogApiFactory.cs) ve [IdentityApiFactory.cs](../../ShuffleSeries.Identity/ShuffleSeries.Identity.IntegrationTests/Infrastructure/IdentityApiFactory.cs) sınıfları; PostgreSQL, Redis ve RabbitMQ container'larını `Task.WhenAll` ile paralel başlatan ve kapatan yapıya dönüştürüldü.
2. **Deterministik Outbox Yürütme Mimarisi (Deadlock Savunması):**
   - Test ortamında otonom 10 saniyelik Quartz HostedService sayacı pasife alındı.
   - [ProcessOutboxMessagesJob](../../ShuffleSeries.Catalog/ShuffleSeries.Catalog.Infrastructure/BackgroundJobs/ProcessOutboxMessagesJob.cs) DI konteynerinde scoped olarak korunarak, testler içerisinden `await TriggerOutboxJobAsync()` ile on-demand (milisaniyelik ve deterministik) çağrılabilir hale getirildi.
   - `context?.CancellationToken ?? CancellationToken.None` koruması eklenerek null-context toleransı sağlandı.
3. **Çift Katmanlı Durum Temizliği (Clean Slate Isolation):**
   - Her test öncesi PostgreSQL `Respawn` ile sıfırlanırken; `allowAdmin=true` yetkili bağlantısıyla Redis `FlushDatabaseAsync()` işletilerek cache ve blacklist alanları temizlendi.
4. **Gerçek Entegrasyon Senaryoları:**
   - Catalog: Redis Cache-Aside doğrulaması ve Outbox -> Real RabbitMQ publish testi.
   - Identity: Redis Token Blacklist doğrulaması ve Outbox -> Real RabbitMQ publish testi.
5. **CI/CD Pipeline Dayanıklılığı (.github/workflows/ci.yml):**
   - `TESTCONTAINERS_RYUK_DISABLED: "true"` eklenerek Ryuk TCP handshake takılmaları engellendi.
   - Docker imajları (`postgres:16-alpine`, `redis:7-alpine`, `rabbitmq:3-management-alpine`) CI başında paralel pre-pull ile çekildi (Docker Hub rate limit savunması).
   - `dotnet test` komutuna `--blame-hang --blame-hang-timeout 2m` ve `timeout-minutes: 12` eklendi.

---

## 2. Neden Yaptık? (Mimari Kararlar ve Teknik Nedenler)

* **Neden Gerçek RabbitMQ & Redis?**
  Entegrasyon testlerinin sözleşmesi, üretim ortamındaki altyapı davranışını birebir test etmektir. Sahte InMemory bus veya mock'lar; serialization anomalilerini, gerçek TCP bağlantı zaman aşımlarını, Redis TTL davranışlarını ve RabbitMQ exchange/routing mekanizmalarını yakalayamaz.
* **Neden Quartz Sayacını Testte Durdurup On-Demand Çağırdık?**
  Otomasyon testlerinde determinizm esastır. Arka planda kör 10 saniyede bir dönen bir job; testlerin veritabanını `TRUNCATE` ettiği anla çakıştığında PostgreSQL üzerinde `AccessExclusiveLock` çekişmesi yaratarak kilitlenmeye (deadlock) yol açar. Ayrıca Coverlet code coverage toplayıcısı aktif thread'ler yüzünden `vstest.console`'u sonlandıramaz. `TriggerOutboxJobAsync()` çağrısı ile test sıfır bekleme süresiyle ve %100 kontrolle koşar.
* **Neden Ryuk'ı CI'da Devre Dışı Bıraktık?**
  GitHub Actions koşucuları (runners) ephemeral'dir; yani test bittiğinde sanal makine tamamen silinir. Testcontainers'ın Ryuk temizlik container'ı CI ortamındaki TCP soket kısıtlarında askıda kalabildiği için `TESTCONTAINERS_RYUK_DISABLED="true"` resmi dokümantasyon tarafından da önerilen altın kuraldır.

---

## 3. Hangi Pattern'leri Kullandık?

1. **Testcontainers Pattern:** Gerçek PostgreSQL, Redis ve RabbitMQ container'larının test yaşam döngüsüyle (`IAsyncLifetime`, `ICollectionFixture`) yönetilmesi.
2. **Transactional Outbox & Deterministic Job Invocation:** Veritabanına atomik yazılan domain event'lerinin, arka plan işleyicisi doğrudan tetiklenerek kuyruğa aktarılması.
3. **Cache-Aside Pattern:** Redis üzerinde taksonomi verilerinin önbelleğe alınması ve `ResetStateAsync` ile izolasyonun sağlanması.
4. **Token Blacklist Pattern:** Identity oturum iptallerinin Redis anahtarları (`shuffleseries:blacklist:user:{userId:N}`) üzerinden doğrulanması.
5. **Fail-Fast & Blame Hang Guard:** CI hattında potansiyel sonsuz döngü ve thread kilitlenmelerinin diagnostik crash dump ile anında yakalanması.

---

## 4. Yapılan Geliştirme Nasıl Test Edilir?

### 4.1. Terminalden Tüm Testlerin Koşturulması
```bash
# Release modunda derle ve coverage ile çalıştır
dotnet test --no-build --configuration Release \
  --settings coverage.runsettings \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults \
  --logger "trx"
```

### 4.2. Test Senaryoları (Test Cases)
1. **Catalog Redis Cache & Flush Testi:**
   - `GET /api/catalog/moods` çağrılır.
   - Redis container'ında `shuffleseries:catalog:metadata:moods` anahtarının oluştuğu doğrulanır.
   - `ResetStateAsync()` çağrıldığında anahtarın silindiği doğrulanır.
2. **Catalog Outbox -> RabbitMQ Testi:**
   - `POST /api/catalog/series` ile dizi eklenir; Outbox tablosunda `ProcessedOnUtc == null` görülür.
   - `TriggerOutboxJobAsync()` çalıştırılır.
   - Mesajın RabbitMQ'ya iletildiği ve `ProcessedOnUtc`'nin dolduğu doğrulanır.
3. **Identity Outbox -> RabbitMQ Testi:**
   - `POST /api/auth/register` ile kullanıcı açılır.
   - `TriggerOutboxJobAsync()` ile `UserRegisteredDomainEvent` RabbitMQ'ya basılır ve Outbox tablosu güncellenir.
4. **Identity Redis Blacklist Testi:**
   - `POST /api/auth/change-password` ile şifre değiştirilir.
   - Redis container'ında `shuffleseries:blacklist:user:{userId:N}` anahtarının varlığı doğrulanır.
