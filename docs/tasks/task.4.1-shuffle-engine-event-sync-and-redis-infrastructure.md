# Task 4.1: Shuffle Engine — Event-Driven Veri Senkronizasyonu ve Redis In-Memory Altyapısı

## 1. Ne Yaptık?

Bu görevde, ShuffleSeries platformunun en yüksek okuma/yazma trafiğini karşılayan çekirdek öneri motoru **`ShuffleSeries.ShuffleEngine`** mikroservisi deterministik 5 katmanlı Onion Architecture standardında üretilmiş; **Merkezi Transactional Inbox**, **RabbitMQ (WolverineFx) Event Choreography**, **Redis Hibrit Bellek-İçi Veri Yapıları (`Sorted Set`, `Hash`, `Set`)** ve **Catalog gRPC Cold-Start Cache Warming** altyapısı uçtan uca hayata geçirilmiştir:

1. **Deterministik Mikroservis Üretimi (`ShuffleSeries.ShuffleEngine`):**
   - `scripts/scaffold_service.py` şablonundaki RabbitMQ bağlantı ayarları HashiCorp Vault `MessageBroker:*` standardıyla eşitlenerek `ShuffleEngine` (`5013` HTTP / `7013` HTTPS) 6 projeli yapısıyla (`Domain`, `Application`, `Infrastructure`, `Api`, `Tests`, `IntegrationTests`) üretildi ve `ShuffleEngineArchitectureTests` ile `MicroserviceArchitectureTestBase` korumasına alındı.
   - `Catalog`, `Identity`, `ShuffleEngine` ve `ApiGateway` mimari testlerinde çift yönlü Bounded Context izolasyonu (`ForbiddenBoundedContextPrefixes`) tanımlandı.

2. **Merkezi Transactional Inbox & Ortak Event Sözleşmeleri (`ShuffleSeries.Shared.Core`):**
   - `Shared.Core.Infrastructure` altına tüm mikroservislerin ortak kullanabileceği `InboxMessageConfiguration` (`IX_InboxMessages_Processed` kısmi indeksi ile) ve yarış durumlarına dayanıklı `InboxProcessor` (**Claim-Execute-Seal** mimarisi: atomik `INSERT ... ON CONFLICT ("Id") DO NOTHING` / `UPDATE` sahiplenme ile dış gRPC/Redis çağrıları boyunca sıfır açık DB bağlantısı + `25P02` güvenli hata kaydı + 7 günlük `ExecuteDeleteAsync` saklama temizliği) eklendi.
   - `Shared.Core.Domain.Events` altında abonelik ve oylama akışları için `UserSubscribedEvent`, `UserSubscriptionCancelledEvent`, `MediaRatedEvent` ve `MediaRatingTier` (`DoubleThumbsUp`, `Liked`, `Disliked`) sözleşmeleri oluşturuldu.

3. **Redis In-Memory Veri Topolojisi (`RedisShufflePoolStore` & `RedisUserShuffleStateStore`):**
   - **Medya & Bölüm Metadata (`HASH`):** `shuffle:media:{id}` ve `shuffle:episode:{id}` anahtarlarında skorlama alanları (`kind`, `imdbRating`, `baseScore`, `moodIds`, `genreIds`, `platformIds`, `director`, `occurredAtTicks`) ayrık hash alanları olarak, tam kart verisi ise UTF-8 byte dizisi (`payload`) olarak saklandı.
   - **Sıralı Aday Havuzları (`ZSET`):** `shuffle:pool:{movies|series|episodes}` üzerinde PRD 6.2 formülüne göre hesaplanan `BaseQualityScore` skoruyla sıralı tutuldu.
   - **Güvenli Başyapıtlar Havuzu (`Safety Net Pool` — `ZSET`):** Yeni ve misafir kullanıcıların soğuk başlangıç deneyimi için `IMDb >= 7.5` olan yapımlar `shuffle:safety-net:{movies|series|episodes}` setlerinde izole edildi.
   - **Çok Boyutlu Filtreleme İndeksleri (`SET`):** Platform (`shuffle:idx:platform:{id}:*`), ruh hali (`shuffle:idx:mood:{id}:*`), klasik tür (`shuffle:idx:genre:{id}:*`) ve dizi-bölüm ters indeksi (`shuffle:idx:series:{seriesId}:episodes`) oluşturuldu.
   - **24 Saatlik Kullanıcı Cooldown Seti (`ZSET` — `cooldown:user:{userId}`):** Pas geçilen içerikler `score = expiresAtUnixSeconds` olarak saklanarak her içeriğin bağımsız 24 saatlik soğuma süresine sahip olması sağlandı.
   - **Kullanıcı İlgi Vektörü (`HASH` — `shuffle:user:{userId}:affinity`):** `MediaRatedEvent` ve onboarding tercihlerinden gelen `+%30 / +%10 / +%15 / -%50` katsayıları Lua script ile atomik olarak `[-0.80, +1.50]` aralığında sınırlandırılıp (clamping) saklandı.
   - **Premium Yetki Önbelleği (`HASH` — `shuffle:user:{userId}:entitlement`):** `UserSubscribedEvent` ve `UserSubscriptionCancelledEvent` mesajlarıyla milisaniyede güncellenen, `occurredAtTicks` zaman damgası kontrolüyle sıra dışı (out-of-order) eski mesajlara karşı korumalı VIP durum önbelleği kuruldu.

4. **Cold-Start Cache Warming & Tekil Bölüm gRPC Senkronizasyonu:**
   - `Catalog.Api/Protos/catalog.proto` üzerine $O(1)$ tekil bölüm senkronizasyonu için `GetStandaloneEpisodeById` RPC'si ve dizi bazlı filtre (`series_id`) eklendi.
   - `ShuffleCacheWarmingService` ve `EnsureShuffleCacheWarmJob` ile Redis boş olduğunda veya `forceRebuild: true` istendiğinde yerel `SemaphoreSlim(1, 1)` + dağıtık Redis kilidi (`StackExchange.Redis` `LockTakeAsync` / `LockReleaseAsync`) altında `Catalog` servisinden `PageSize = 100` sayfalı akışla tüm katalog ve bağımsız bölümler iki aşamalı `IBatch` pipeline üzerinden Redis'e dolduruldu; `forceRebuild: true` modunda ise `PruneStaleEntriesAsync` ile çevrimdışı silinmiş hayalet kayıtlar 3 sabit turlu pipeline ile süpürüldü.

---

## 2. Neden Yaptık? (Mimari Kararlar ve Gerekçeler)

1. **Neden İlişkisel Katalog Kopyası Yerine Saf Redis In-Memory Havuzu (Ponytail YAGNI)?**
   Rulet motoru her çark çevirmede kullanıcının platformları, seçtiği ruh hali, kütüphane muafiyetleri ve 24 saatlik Cooldown seti arasında kesişim alıp ağırlıklı örnekleme yapar. PostgreSQL üzerinde çoklu `JOIN` ve `ORDER BY RANDOM()` çalıştırmak yüksek eşzamanlılıkta darboğaz üretir. Katalog verisinin tek gerçeklik kaynağı zaten `Catalog` servisidir; `ShuffleEngine` ise bu veriyi Redis `SET`/`ZSET`/`HASH` yapılarında tutarak $< 2\text{ms}$ yanıt süresi sağlar ve PostgreSQL'i yalnızca hafif ACID `InboxMessages` / `OutboxMessages` tabloları için kullanır.
2. **Neden `cooldown:user:{userId}` İçin Düz `SET` Yerine Zaman Skorlu `ZSET` Seçtik?**
   Redis'te `SET` elemanlarına tekil TTL verilemez; yalnızca anahtarın tamamına `EXPIRE` atanabilir. Kullanıcı sabah 10:00'da A filmini, akşam 22:00'de B filmini pas geçtiğinde düz `SET` kullanılsaydı ya A filminin soğuma süresi her yeni pas geçmede sonsuza dek uzayacak ya da B filmi erken düşecekti. `ZSET` içinde `score = expiresAtUnixSeconds` tutulduğunda `ZRANGEBYSCORE key now +inf` sorgusu yalnızca 24 saati dolmamış içerikleri tam isabetle dönerken, anahtar seviyesindeki `24s + jitter` TTL ise pasif kullanıcı anahtarlarının bellekte kalmasını önler.
3. **Neden Kademeli (Staggered) TTL Mimarisi (`WarmMarker = 12s` vs `Pool = 24s`)?**
   Performans anayasası gereği Redis'te sonsuz ömürlü dinamik anahtar bırakılamaz. Ancak havuz anahtarları ile ısınma işaretçisi (`shuffle:meta:warmed`) aynı anda düşerse okuma anında boş havuz (Cache Miss) riski doğar. İşaretçi anahtara `12 saat + jitter`, veri ve indeks anahtarlarına ise `24 saat + jitter` verilerek periyodik Quartz işinin veriler silinmeden 12 saat önce havuzu arka planda kesintisiz yenilemesi garanti altına alınmıştır.
4. **Neden Tüketiciye Özel Internal Protobuf Sözleşmesi (ACL)?**
   `ShuffleEngine.Infrastructure` kendi `Protos/catalog.proto` istemci şemasını (`Access="Internal"`, `csharp_namespace = "ShuffleSeries.ShuffleEngine.Infrastructure.Grpc.Catalog"`) derler. Protobuf tel (wire) protokolü yalnızca `package catalog.v1;` ve `service CatalogGrpc` adını kullandığından `Catalog.Api` ile %100 ikili uyumluluk korunurken, derleme zamanında servisler arası assembly/namespace bağımlılığı sıfırlanır.

---

## 3. Hangi Pattern'leri Kullandık?

| Tasarım Deseni (Pattern) | Sistemdeki İşlevi |
| :--- | :--- |
| **Transactional Inbox Pattern** | PostgreSQL `ON CONFLICT DO NOTHING` + `FOR UPDATE` kilidi ile RabbitMQ mükerrer mesajlarının iki kez işlenmesini engeller. |
| **Event Choreography** | Merkezi orkestratör olmadan `Catalog`, `Identity`, `Subscription` ve `UserLibrary` olaylarının asenkron tüketilmesi. |
| **Redis Pipelining (`IBatch`) & Lua Scripting** | Yüzlerce `HSET`/`ZADD`/`SADD` komutunu tek TCP turunda yürütür; afinite ve abonelik güncellemelerini atomik kılar. |
| **Optimistic Version Guard (`occurredAtTicks`)** | Kuyruktan sıra dışı (out-of-order) gelen gecikmiş olayların daha yeni Redis durumunu ezmesini engeller. |
| **Distributed Lock (`SET NX PX` + Lua)** | Çoklu pod Cold-Start anında `Catalog` gRPC servisine tek bir pod'un bağlanmasını sağlar (Cache Stampede koruması). |
| **Consumer-Side Anti-Corruption Layer (gRPC)** | Protobuf modellerini saf domain anlık görüntülerine (`ShuffleMediaSnapshot`, `ShuffleEpisodeSnapshot`) dönüştürür. |

---

## 4. Yapılan Geliştirme Nasıl Test Edilir?

### A. Mimari, Birim ve Gerçek Testcontainers Entegrasyon Testleri
```bash
dotnet format ShuffleSeries.sln --verify-no-changes
dotnet build ShuffleSeries.sln --configuration Release
TESTCONTAINERS_RYUK_DISABLED=true dotnet test ShuffleSeries.sln --configuration Release --blame-hang --blame-hang-timeout 2m
```

### B. Canlı Docker Compose & YARP API Gateway E2E Test Paketi
```bash
docker compose -f docker-compose.yml -f docker-compose.apps.yml up -d --build
python3 e2e_tests/run_all.py
```

### C. Doğrulanan Temel Test Senaryoları (Test Cases)
1. **Cold-Start Cache Warming (`POST /api/shuffle/admin/warm-cache`):** Boş Redis üzerinde `CatalogGrpcService` üzerinden tüm film, dizi ve bağımsız bölümlerin çekilmesi; `IMDb < 7.5` içeriklerin ana havuza girerken `shuffle:safety-net:*` havuzundan dışlanması ve tüm Redis anahtarlarının pozitif TTL taşıması.
2. **İdempotent Isınma Kontrolü:** Havuz sıcakken `forceRebuild: false` ile çağrıldığında gRPC taraması yapılmadan `wasSkipped: true` dönülmesi.
3. **Mükerrer Mesaj Koruması (InBox Idempotency):** Aynı `Event.Id` değerine sahip `MediaRatedEvent` iki kez gönderildiğinde kullanıcı afinite ağırlığının `+%60` değil yalnızca bir kez (`+%30`) artması ve `InboxMessages` kaydının `IsProcessed = true` olarak mühürlenmesi.
4. **Stale Index & Ghost Episode Temizliği:** Bir dizinin platform/mood ilişkisi güncellendiğinde veya puanı `7.5` altına düştüğünde eski set üyeliklerinin silinmesi; dizi silindiğinde (`MediaDeletedEvent`) bağlı bağımsız bölümlerin de Bölüm Ruleti havuzundan atomik çıkarılması.
5. **Sıra Dışı Abonelik Koruması & GDPR Temizliği:** `T2` anındaki `UserSubscriptionCancelledEvent` sonrasında gecikmeli gelen `T0` (`T0 < T2`) tarihli `UserSubscribedEvent`'in iptal durumunu ezmemesi; `UserAccountDeletedEvent` geldiğinde kullanıcının tüm Redis anahtarlarının silinmesi.
6. **Edge & RBAC Güvenliği:** `/api/shuffle/admin/*` uç noktalarında anonim isteklerin `401 AUTH_UNAUTHORIZED`, `Guest`/`Standard` rolündeki kullanıcıların `403 AUTH_FORBIDDEN`, `Admin` rolünün `200 OK` alması.
