# Task 3.3: gRPC İletişim Uç Noktası (`CatalogGrpcService` — Shuffle Engine Entegrasyonu)

## 1. Ne Yaptık?
Bu görevde, `ShuffleSeries.ShuffleEngine` mikroservisinin Redis Cold Start / Cache Warming ve Bölüm Ruleti (Episode Roulette) beslemesini milisaniye seviyesinde ve minimum ağ yüküyle gerçekleştirebilmesi için Catalog servisi üzerinde contract-first **Protobuf / gRPC** sunucu katmanı ve buna bağlı CQRS altyapısı inşa edildi:

1. **Protobuf Sözleşmesi (`ShuffleSeries.Catalog.Api/Protos/catalog.proto`):**
   - `catalog.v1.CatalogGrpc` servisi ve üç temel RPC metodu tanımlandı:
     - `GetBulkMediaForShuffle`: Sayfalı (`page_number`, `page_size`) ve isteğe bağlı `media_type` (`Movie = 1`, `Series = 2`) filtreli toplu medya senkronizasyonu.
     - `GetStandaloneEpisodesForRoulette`: `IsStandalone = true` bağımsız bölümlerin üst dizi bağlamıyla (mood/genre/platform listeleri ve dizi çevirileri) birlikte **Paginated Unary RPC** (`page_number`, `page_size` ➔ `items`, `total_count`, `page_number`, `page_size`) standardında sayfalı çekilmesi.
     - `GetMediaById`: `MediaCreatedEvent` / `MediaUpdatedEvent` veya tekil senkronizasyonlar için ID bazlı tekil medya sorgusu.
   - Çok dilli içerikler için sözleşmede `map<string, LocalizedMediaPayload>` ve `map<string, LocalizedEpisodePayload>` yapıları kullanıldı.
   - Platform derin bağlantıları (`PlatformDeepLinkGrpcModel`: `web_url`, `app_scheme_url`, `platform_slug`, `logo_url`) ve taksonomi ID listeleri (`platform_ids`, `mood_ids`, `genre_ids`) modele dahil edildi.

2. **Application Katmanı CQRS Sorguları & `Shared.Core` Yeniden Kullanımı (`Features/ShuffleSync/`):**
   - **1-Class-1-File** kuralına uygun olarak 5 response record'u (`ShuffleMediaItemResponse`, `ShuffleStandaloneEpisodeResponse`, `LocalizedMediaPayloadDto`, `LocalizedEpisodePayloadDto`, `ShufflePlatformLinkDto`) ve 3 CQRS Query/Handler çifti (`GetBulkMediaForShuffleQuery`, `GetStandaloneEpisodesForRouletteQuery`, `GetMediaByIdForShuffleQuery`) oluşturuldu.
   - Sayfalı sorgu ve yanıt sözleşmeleri için sıfırdan sarmalayıcı yazmak yerine `ShuffleSeries.Shared.Core.Application` altındaki `PaginationRequest` (`DefaultPageNumber = 1`, `DefaultPageSize = 10`, `MaxPageSize = 100`) ve `PaginatedList<T>` yapıları doğrudan yeniden kullanıldı; 100'den fazla kayıt standart çoklu sayfa döngüsüyle (`page_number = 1..⌈total_count / page_size⌉`) çekilir.

3. **Domain & Infrastructure Performans Optimizasyonları (`MediaRepository` & `EpisodeRepository`):**
   - `IMediaRepository.GetByIdForShuffleAsync` ve `GetBulkForShuffleAsync`: `.AsNoTracking()`, `.AsSplitQuery()`, mevcut Primary Key B-Tree indeksini kullanan deterministik `OrderBy(x => x.Id)` sıralaması ve soft-deleted taksonomi (`Platform`, `Mood`, `Genre`) sızıntısını SQL aşamasında engelleyen filtrelenmiş `.Include(...Where(... != null))` sorgularıyla sıfır ChangeTracker maliyeti ve tam veri bütünlüğü sağlandı.
   - `IEpisodeRepository.GetStandaloneEpisodesWithSeriesContextAsync`: Bağımsız bölümlerin üst sezon ve dizilerinin soft-deleted olup olmadığını SQL seviyesinde filtreleyerek "Ghost Active Episodes" sızıntısını önledi; `ApplyPagination(page, pageSize)` ile sayfalı çekim yaptıktan sonra benzersiz `SeasonId` ve `SeriesId` kümeleri üzerinden **tek seferlik toplu sorgular** atıp RAM'de $O(1)$ `Dictionary` eşlemesiyle üst dizi bağlamını birleştirdi (Zero Loop I/O / Sıfır N+1). Bölümün kendi aktif `EpisodePlatforms` kaydı yoksa üst dizinin aktif `MediaPlatforms` listesine fallback yapar.

4. **gRPC Sunum Katmanı, Kestrel HTTP/2 (`h2c`) & Güvenlik (`CatalogGrpcService`):**
   - `CatalogGrpcService`, doğrudan repository veya `DbContext` yerine yalnızca Wolverine `IMessageBus` kullanarak Clean Architecture sınırlarını korudu.
   - `AddGrpc(options => options.MaxReceiveMessageSize = 1 * 1024 * 1024)` ile payload boyutu sınırlandı; geçersiz GUID veya geçersiz `media_type` için `StatusCode.InvalidArgument`, bulunamayan medya için `StatusCode.NotFound`, beklenmeyen hatalarda ise iç detay sızdırmayan `StatusCode.Internal` dönüldü.
   - Docker (`docker-compose.apps.yml`) ve yerel çalışma zamanında Kestrel üzerinde HTTP/1.1 REST (`8080` -> `5010`) ile HTTP/2 cleartext gRPC (`8081` -> `5012`) ayrımı yapıldı. YARP Gateway üzerinde gRPC rotası dış dünyaya açılmadı (Edge Isolation).

5. **Kapsamlı Test ve Doğrulama:**
   - **Unit Tests (`ShuffleSeries.Catalog.Tests`):** Handler ve InMemory repository testleri (`GetBulkMediaForShuffleQueryHandlerTests`, `GetStandaloneEpisodesForRouletteQueryHandlerTests`, `GetMediaByIdForShuffleQueryHandlerTests`, `MediaRepositoryTests`, `EpisodeRepositoryTests`).
   - **Architecture Tests (`ShuffleSeries.ArchitectureTests`):** `GrpcServices_ShouldBeSealed_And_NotDirectlyDependOn_RepositoriesOrDbContext` mimari fitness kuralı.
   - **Real Integration Tests (`ShuffleSeries.Catalog.IntegrationTests`):** Testcontainers (PostgreSQL, Redis, RabbitMQ) ve `GrpcChannel` (`Factory.Server.CreateHandler()`) ile 8 uçtan uca gRPC entegrasyon testi.
   - **Black-Box E2E Tests (`e2e_tests/grpc_client.py` & `test_catalog_api_gateway.py`):** Canlı Docker container'ına (`127.0.0.1:5012`) HTTP/2 `h2c` + Protobuf çağrıları ve Gateway Edge Isolation testi.

---

## 2. Neden Yaptık?
1. **Cold Start & Yüksek Hacimli İçerik Senkronizasyonu:** Shuffle Engine Redis kümesi sıfırlandığında binlerce film/dizi/bölümü ve çok dilli çevirilerini tek seferde çekmelidir. HTTP/2 multiplexing ve binary Protobuf serileştirmesi, REST JSON'a göre ağ bant genişliğini ve GC/serileştirme maliyetini dramatik olarak düşürür.
2. **Temiz Mimari Sınırları:** gRPC servislerinin doğrudan ORM'e bağlanması Wolverine middleware'lerini (`LoggingMiddleware`, `PerformanceMiddleware`) ve CQRS ayrımını baypas ederdi. `IMessageBus` üzerinden akış sayesinde HTTP ve gRPC uçları aynı Application iş kurallarını paylaşır.
3. **Kestrel `h2c` Çift Port Ayrımı:** TLS sonlandırması Gateway/Ingress katmanında yapıldığı için iç ağdaki HTTP portlarında ALPN müzakeresi yoktur. Tek portta `Http1AndHttp2` HTTP/2'yi düşürebileceğinden, REST (`Http1`) ve iç gRPC (`Http2`) portlarının ayrılması deterministik bağlantı sağlar.
4. **ARM64 Docker Derleme Kararlılığı:** `Grpc.Tools` 2.71.0 sürümü `linux_arm64` konteynerlerinde `protoc` SIGSEGV (exit code 139) hatası verdiği için `Grpc.AspNetCore` / `Grpc.Net.Client` `2.85.0` sürümü tercih edildi.

---

## 3. Hangi Pattern'leri Kullandık?
| Pattern | Kullanım Amacı |
| :--- | :--- |
| **Contract-First gRPC (Protobuf v3)** | İç servisler arası (`ShuffleEngine` ➔ `Catalog`) katı şemalı, düşük gecikmeli binary RPC iletişimi. |
| **CQRS & Mediator (`WolverineFx`)** | gRPC sunum katmanının Repository/DbContext erişimini engelleyip okuma sorgularını izole etmesi. |
| **Zero Loop I/O + Split Query (`AsSplitQuery`)** | Bağımsız bölümlerin üst sezon/dizi bağlamını N+1 sorgu ve Kartezyen patlama olmadan toplu çekmesi. |
| **Pagination Clamping (`PaginationRequest`)** | Büyük sayfa veya negatif offset saldırılarına karşı `MaxPageSize = 100` DoS koruması. |
| **Edge Isolation (Zero-Trust Gateway)** | İç gRPC endpoint'inin YARP API Gateway üzerinden dış internete kapatılması. |

---

## 4. Yapılan Geliştirme Nasıl Test Edilir?

### A. Mimari ve Birim Testleri
```bash
dotnet test ShuffleSeries.ArchitectureTests/ShuffleSeries.ArchitectureTests.csproj
dotnet test ShuffleSeries.Catalog/ShuffleSeries.Catalog.Tests/ShuffleSeries.Catalog.Tests.csproj
```

### B. Gerçek Altyapı Entegrasyon Testleri (Testcontainers PostgreSQL + Redis + RabbitMQ)
```bash
dotnet test ShuffleSeries.Catalog/ShuffleSeries.Catalog.IntegrationTests/ShuffleSeries.Catalog.IntegrationTests.csproj --filter "FullyQualifiedName~CatalogGrpcServiceIntegrationTests"
```
**Doğrulanan Senaryolar:**
- `GetBulkMediaForShuffle`: Tohumlanan film (`Inception`) ve dizi (`Black Mirror`) kayıtlarının `tr` / `en` çeviri sözlükleri, platform deep link'leri, mood ve genre ID'leriyle eksiksiz dönmesi; `MediaType` filtreleme, negatif/aşırı sayfa boyutlarının clamp edilmesi ve geçersiz `MediaType` için `InvalidArgument`.
- `GetStandaloneEpisodesForRoulette`: Antoloji bölümlerinin (`San Junipero` vb.) üst dizi `MoodIds`, `GenreIds` ve `SeriesTranslations` ile dönmesi; üst dizisi soft-deleted olan bölümlerin havuza sızmaması.
- `GetMediaById`: Geçerli ID ile 200/OK, olmayan GUID ile `StatusCode.NotFound`, boş/bozuk GUID ile `StatusCode.InvalidArgument`.

### C. Canlı Docker Compose & Black-Box E2E Testleri
```bash
docker compose -f docker-compose.yml -f docker-compose.apps.yml up -d --build
python3 e2e_tests/run_all.py
```
**E2E Test Senaryoları (Section 12):**
- `POST /catalog.v1.CatalogGrpc/GetMediaById` (Gateway `5001`) ➔ `404 Not Found` (Edge Isolation).
- `GetBulkMediaForShuffle` (`127.0.0.1:5012`) ➔ gRPC Status `0`, `total_count >= 2`, çok dilli `translations`.
- `GetStandaloneEpisodesForRoulette` (`127.0.0.1:5012`) ➔ gRPC Status `0`, aktif bağımsız bölümler + dizi çevirileri.
- `GetMediaById` (`127.0.0.1:5012`) ➔ Geçerli ID (`0`), Olmayan ID (`5 NOT_FOUND`), Geçersiz GUID (`3 INVALID_ARGUMENT`).
