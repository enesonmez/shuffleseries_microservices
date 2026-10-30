# Task 3.2.3: Catalog Performance Guard Optimizasyonları (Cache Stampede, Over-Fetching, ChangeTracker & Outbox Indexing)

Bu dokümanda, `ShuffleSeries.Catalog` mikroservisinde `/performance-guard` incelemesi sonucunda tespit edilen mimari darboğazların çözümü, uygulanan tasarım desenleri ve test stratejileri açıklanmaktadır.

---

## 1. Ne Yaptık?
1. **Cache Stampede (Thundering Herd) & Avalanche Savunması:**
   - [`MemoryCatalogCacheService`](../../ShuffleSeries.Catalog/ShuffleSeries.Catalog.Infrastructure/Services/MemoryCatalogCacheService.cs) içine `ConcurrentDictionary<string, SemaphoreSlim>` ile anahtar bazlı eşzamanlı kilit mekanizması eklendi.
   - Cache hit durumları lock-free $O(1)$ bırakıldı; cache miss anında `Double-Checked Locking` deseniyle yüzlerce eşzamanlı isteğin veritabanına aynı anda yüklenmesi önlendi.
   - Önbellek anahtarlarına $+ \text{Random}(0, 180\text{s})$ sapma (TTL Jitter) eklenerek tüm anahtarların aynı anda düşmesi engellendi.
2. **`CreateEpisodeCommandHandler` Over-Fetching Eliminasyonu:**
   - [`ISeriesRepository`](../../ShuffleSeries.Catalog/ShuffleSeries.Catalog.Domain/Repositories/ISeriesRepository.cs) ve `SeriesRepository`'ye yalnızca sezon ve bölümleri eager-load eden [`GetByIdWithSeasonsAsync`](../../ShuffleSeries.Catalog/ShuffleSeries.Catalog.Infrastructure/Persistence/Repositories/SeriesRepository.cs) metodu eklendi.
   - Tek bir bölüm eklenirken dizinin tüm çevirilerini, platformlarını, türlerini ve mood'larını yükleyen 5-6 split query kaldırıldı.
3. **ChangeTracker İzolasyonu (`asNoTracking` Desteği):**
   - [`MovieRepository`](../../ShuffleSeries.Catalog/ShuffleSeries.Catalog.Infrastructure/Persistence/Repositories/MovieRepository.cs) ve [`EpisodeRepository`](../../ShuffleSeries.Catalog/ShuffleSeries.Catalog.Infrastructure/Persistence/Repositories/EpisodeRepository.cs) üzerinde `asNoTracking: true/false` desteği getirildi.
   - [`GetMovieByIdQueryHandler`](../../ShuffleSeries.Catalog/ShuffleSeries.Catalog.Application/Features/Movies/Queries/GetMovieById/GetMovieByIdQueryHandler.cs) ve [`GetEpisodeByIdQueryHandler`](../../ShuffleSeries.Catalog/ShuffleSeries.Catalog.Application/Features/Episodes/Queries/GetEpisodeById/GetEpisodeByIdQueryHandler.cs) salt okuma handler'larında `asNoTracking: true` çağrılarak ChangeTracker bellek snapshot yükü sıfırlandı.
4. **Media Association Komutlarında Odaklı Eager-Loading:**
   - [`MediaRepository`](../../ShuffleSeries.Catalog/ShuffleSeries.Catalog.Infrastructure/Persistence/Repositories/MediaRepository.cs) içine `GetByIdWithPlatformsAsync`, `GetByIdWithMoodsAsync`, `GetByIdWithGenresAsync` ve `GetByIdWithTranslationsAsync` metotları tanımlandı.
   - Tek bir platform, mood, genre veya çeviri eklenip çıkarılırken tüm aggregate ağacının yüklenmesi engellendi.
5. **Outbox İndeks Optimizasyonu ve EF Core Migrasyonu:**
   - [`OutboxMessageConfiguration`](../../ShuffleSeries.Catalog/ShuffleSeries.Catalog.Infrastructure/Persistence/Configurations/OutboxMessageConfiguration.cs) güncellenerek partial indeks `(OccurredOnUtc) WHERE "ProcessedOnUtc" IS NULL AND "RetryCount" < 3` oluşturuldu (`OptimizeOutboxMessagesIndex` migrasyonu üretildi).
6. **MassTransit Prefetch Sınırlandırması & TimeProvider Standardı:**
   - RabbitMQ bus konfigürasyonuna Pod OOM koruması için `PrefetchCount = 16` eklendi.
   - [`ProcessOutboxMessagesJob`](../../ShuffleSeries.Catalog/ShuffleSeries.Catalog.Infrastructure/BackgroundJobs/ProcessOutboxMessagesJob.cs) içerisinde `.NET 10 TimeProvider` standardına geçildi.

---

## 2. Neden Yaptık? (Mimari Gerekçeler)
* **Neden Cache Stampede Kilidi?**
  Çok popüler bir içerik veya taksonomi anahtarının süresi dolduğunda, saniyede gelen binlerce istek aynı anda veritabanına koşarak bağlantı havuzunu (connection pool) tüketir ve veritabanını felç eder. Double-checked locking ile sadece 1 thread veritabanına gidip cache'i doldurur, diğerleri anında bellekten döner.
* **Neden Odaklı Repository Metotları?**
  DDD'de Aggregate Root sınırları korunmalıdır; ancak her aggregate operasyonu için tüm aggregate'in en derin dallarına kadar (deep graph) SQL JOIN / Split Query ile belleğe çekilmesi büyük veritabanlarında CPU ve RAM darboğazı yaratır. Yalnızca ilgili operasyonun invariant'larını (iş kurallarını) korumaya yetecek alt-grafın çekilmesi sektör standardıdır.
* **Neden Outbox Partial Composite Index?**
  Mevcut indekste `ProcessedOnUtc` kolonu filtrelenirken kolonun kendisi indeksleniyordu. İndeksteki tüm satırların değeri `NULL` olduğundan kolonun seçiciliği (cardinality) sıfırdı ve sıralama (ORDER BY OccurredOnUtc) için ayrı bellek operasyonu gerekiyordu. `(OccurredOnUtc) WHERE ProcessedOnUtc IS NULL AND RetryCount < 3` indeksi ile PostgreSQL tek seferde Index-Only Scan yapar ve sıralamayı bedavaya getirir.

---

## 3. Hangi Pattern'leri Kullandık?
* **Cache-Aside with Double-Checked Locking:** Önce kilitsiz $O(1)$ bellek kontrolü, ıskalamada per-key `SemaphoreSlim` kilidi ve kilit içinde tekrar bellek kontrolü.
* **Jittered Expiration Pattern:** Deterministik sürelere rastgele gecikme ekleyerek önbellek çığını önleme.
* **Read-Only ChangeTracker Isolation:** CQRS Query handler'larında ChangeTracker maliyetini sıfırlama (`AsNoTracking`).
* **Focused Aggregate Boundary Loading:** Mutasyon gereksinimine göre aggregate sınırlarının minimal I/O ile yüklenmesi.

---

## 4. Nasıl Test Edilir?
* **Birim Testleri:**
  ```bash
  dotnet test ShuffleSeries.Catalog/ShuffleSeries.Catalog.Tests/
  ```
  `MemoryCatalogCacheServiceTests` altında eşzamanlı 10 isteğin aynı anahtara saldırması (`GetOrCreateAsync_Should_PreventCacheStampede_WhenMultipleConcurrentRequestsHitSameKey`) ve factory'nin kesinlikle yalnızca 1 kez çalıştığı doğrulanır.
* **Entegrasyon Testleri (Gerçek PostgreSQL):**
  ```bash
  dotnet test ShuffleSeries.Catalog/ShuffleSeries.Catalog.IntegrationTests/
  ```
* **Tüm Çözüm Testleri:**
  ```bash
  dotnet test ShuffleSeries.sln
  ```
