# Task 3.2.7: Catalog Distributed Redis Cache Migration & Tak-Çıkar Mimari

## 1. Ne Yaptık? (Genel Özet)
Catalog mikroservisinde taksonomi ve referans verilerinin (`Platforms`, `Moods`, `Genres`) hızlı okunması için daha önce `IMemoryCache` (yerel sunucu belleği) tabanlı bir önbellekleme kullanılıyordu. Bu aşamada:

1. **Dağıtık Önbellek Mimarisine Geçiş (`RedisCatalogCacheService`):**
   * Tüm taksonomi okuma sorgularını (`GetActivePlatformsQuery`, `GetActiveMoodsQuery`, `GetActiveGenresQuery`) ve ilgili `GetById` fallback'lerini `StackExchange.Redis` tabanlı merkezi önbellek kümesine taşıdık.
2. **Tak-Çıkar (Plug-and-Play / LSP) Arayüz Sözleşmesi:**
   * `ICatalogCacheService` arayüzüne `Task RemoveAsync(string key, CancellationToken cancellationToken = default);` metodunu ekledik.
   * `MemoryCatalogCacheService` sınıfına da `RemoveAsync` implementasyonu eklenerek, Dependency Injection katmanında tek bir satır değişikliğiyle (`services.AddSingleton<ICatalogCacheService, RedisCatalogCacheService>();` <-> `MemoryCatalogCacheService`) sıfır kod değişikliği ile iki önbellek motoru arasında tam uyum sağlandı.
3. **13 Command Handler'ın Asenkron Cache Invalidation Dönüşümü:**
   * Mood, Genre ve Platform varlıklarının oluşturma, güncelleme, silme ve çeviri yönetimini yapan 13 adet CQRS Command Handler'ındaki senkron `_cacheService.Remove(...)` çağrıları `await _cacheService.RemoveAsync(..., cancellationToken)` saf asenkron standardına dönüştürüldü.
4. **Dağıtık Sistem Dayanıklılığı (Resilience & Scalability Guard):**
   * **Fail-Open Resilience:** Redis sunucusuna erişilemediğinde (`RedisConnectionException`, `TimeoutException`, `SocketException`) uygulamanın 500 fırlatmasını önleyen, uyarı loglayıp veriyi doğrudan veritabanı factory'sinden çeken koruma katmanı kuruldu.
   * **Cache Stampede (Thundering Herd) Koruması:** Anahtar bazlı `ConcurrentDictionary<string, SemaphoreSlim>` ile double-checked locking uygulanarak, aynı anahtarın düşmesi anında eşzamanlı gelen binlerce isteğin veritabanını boğması engellendi.
   * **Cache Avalanche Koruması:** Deterministik TTL üzerine $+ \text{rand}(0, 180\text{s})$ rastgele sapma (TTL Jitter) eklenerek tüm anahtarların aynı anda düşüp veritabanına ani yük bindirmesi önlendi.
5. **Konteyner Hazır Bulunuşluk Sağlık Kontrolü (Readiness Health Check):**
   * Catalog API `/health/ready` probe'una `RedisHealthCheck` (`builder.Services.AddSharedRedisHealthCheck();`) dahil edildi. Redis ping gecikmesi milisaniye hassasiyetinde (`latencyMs: ~0.3ms`) yapılandırılmış JSON verisi olarak Kubernetes/Gateway seviyesine sunuldu.
6. **Serileştirilebilir Cache DTO Modelleri ve Vertical Slice İzolasyonu:**
   * DDD Domain Entity'lerinin (`Mood`, `Genre`) parametresiz private constructor ve zengin iş kuralları nedeniyle `System.Text.Json` tarafından doğrudan deserialize edilememesi problemine karşı, önbelleğe özel hafif serileştirme modelleri (`CachedMoodItem`, `CachedGenreItem`) oluşturuldu.
   * Bu modeller handler'ların içinden çıkarılarak `Features/{Moods|Genres}/Models/` altında bağımsız dosyalara taşındı (SRP & Vertical Slice temizliği).
7. **Sır Yönetimi & HashiCorp Vault SSOT Güvencesi:**
   * `docker-compose.apps.yml` dosyasındaki hardcoded veritabanı, redis ve broker ortam değişkenleri temizlendi.
   * `Vault__Optional=false` tanımlanarak mikroservislerin konfigürasyonlarını yalnızca HashiCorp Vault üzerinden alması garanti altına alındı (Fail-Fast prensibi).
8. **Kapsamlı Test ve Doğrulama:**
   * Yeni `RedisCatalogCacheServiceTests` sınıfı ile Cache Hit, Cache Miss, Cache Stampede, Redis Hatası (Fail-Open) ve Remove testleri yazıldı.
   * Çözümdeki 850 adet birim ve entegrasyon testi ile API Gateway üzerinden koşan 123 adet uçtan uca (E2E) test %100 başarıyla doğrulandı.

---

## 2. Neden Yaptık? (Mimari Kararlar ve Teknik Gerekçeler)

### 2.1 Multi-Replica Stale Read / Cache Inconsistency Problemi
Tek bir sunucuda çalışırken `IMemoryCache` sıfır ağ gecikmesi ($0\text{ms}$) ile mükemmel bir çözüm sunar. Ancak mikroservis mimarilerinde yüksek erişilebilirlik ve yük dengeleme (Horizontal Pod Autoscaler - HPA) gereği Catalog servisi en az 2 kopya (Pod A ve Pod B) olarak kaldırılır:
* **Senaryo:** Bir yönetici admin paneli üzerinden yeni bir platform eklediğinde veya bir ruh halinin adını güncellediğinde, API Gateway bu isteği Pod A'ya yönlendirir. Pod A veritabanını günceller ve kendi yerel `IMemoryCache`'ini temizler.
* **Sorun:** Pod B'nin bu mutasyondan haberi yoktur. Kullanıcının bir sonraki okuma isteği Pod B'ye düştüğünde, Pod B kendi yerel belleğindeki eski (stale) veriyi döner. Kullanıcı aynı sayfayı yeniledikçe yük dengeleyicinin (Round-Robin) seçimine göre bazen yeni veriyi bazen eski veriyi görür.
* **Çözüm:** Önbelleği process-bound (işlem içi) yerel bellekten ayırıp bağımsız ve merkezi bir **Distributed In-Memory Store (Redis)** kümesine taşımaktır. Pod A anahtarı sildiğinde (`RemoveAsync`), Pod B'nin de okuyacağı merkezi anahtar silinmiş olur ve anında nihai tutarlılık (Read-After-Write Consistency) sağlanır.

### 2.2 Neden RabbitMQ Fanout Exchange veya Redis Pub/Sub Yerine Doğrudan Dağıtık Cache Seçildi?
Her pod'un kendi `IMemoryCache`'ini tuttuğu ve mutasyonlarda RabbitMQ Fanout veya Redis Pub/Sub ile diğer pod'lara "önbelleği temizle" mesajı fırlattığı karma (hybrid/distributed-invalidation) model teoride mümkündür; ancak şu mimari riskleri barındırır:
1. **Network Partition (Ağ Bölünmesi):** Geçici bir ağ kesintisinde bir pod mesajı kaçırırsa, o pod yeniden başlatılana kadar sonsuza kadar bayat veri sunar.
2. **Mimari Yük ve Aşırı Mühendislik (Over-Engineering):** Yalnızca 3 adet taksonomi listesi için fanout exchange'ler, tüketici kuyrukları ve pub/sub dinleyicileri kurmak kod karmaşıklığını ve bakım maliyetini gereksiz yere artırır.
3. **Merkezi Redis Güvencesi:** Projemizin teknoloji yığınında zaten yüksek erişilebilir bir Redis kümesi bulunmaktadır. Doğrudan Redis üzerinde `GetOrCreateAsync` işletmek hem $0.3\text{ms}$ düzeyinde olağanüstü hızlıdır hem de mimariyi en sade (Ponytail) haliyle tutar.

### 2.3 Tak-Çıkar (Plug-and-Play) ve Liskov Substitution Prensibi (LSP)
SOLID'in Liskov Değiştirilebilirlik Prensibi uyarınca, bir arayüzü (`ICatalogCacheService`) uygulayan sınıflar (`MemoryCatalogCacheService` ve `RedisCatalogCacheService`), çağıran sınıfların (Command/Query Handler'lar) davranışını bozmadan birbirinin yerine geçebilmelidir.
* `MemoryCatalogCacheService` içine de `RemoveAsync` implementasyonu eklenerek, geliştirme ortamında (örneğin internetin veya yerel Redis'in olmadığı durumlarda) tek bir DI satırıyla bellek içi önbelleğe geri dönülebilmesi garanti altına alınmıştır:
```csharp
// Redis İstenirse:
services.AddSingleton<ICatalogCacheService, RedisCatalogCacheService>();

// Bellek İçi İstenirse (Zero-Dependency Local Dev):
// services.AddSingleton<ICatalogCacheService, MemoryCatalogCacheService>();
```

### 2.4 Fail-Open Resilience (Kesintisiz Kullanılabilirlik)
Önbellek katmanı bir hızlandırıcıdır (accelerator); tek hata noktası (Single Point of Failure - SPOF) olmamalıdır. Redis sunucusu yeniden başlasa veya geçici bağlantı hatası verse dahi, istemcinin `500 Internal Server Error` alması kabul edilemez.
`RedisCatalogCacheService`, tüm Redis çağrılarını `try-catch` blokları içinde yönetir:
```csharp
catch (Exception ex) when (ex is RedisException or TimeoutException or SocketException)
{
    _logger.LogWarning(ex, "Redis cache read failed for key {CacheKey}. Falling back to database.", formattedKey);
}
```
Hata durumunda log yazılır ve akış doğrudan veritabanı fonksiyonuna (`factory()`) yönlendirilir.

---

## 3. Hangi Pattern'leri Kullandık?

| Tasarım Deseni | Sistemdeki İşlevi |
| :--- | :--- |
| **Distributed Caching Pattern** | Çoklu sunucu/pod ortamında önbellek tutarsızlığını (stale read) önleyen merkezi StackExchange.Redis veri saklama yapısı. |
| **Read-Through Caching** | Veri önce Redis'ten sorgulanır; bulunamazsa factory üzerinden veritabanından çekilip Redis'e yazılır ve çağırana teslim edilir. |
| **Double-Checked Locking with SemaphoreSlim** | Cache Stampede (Thundering herd) atağına karşı anahtar bazlı eşzamanlı istekleri sıraya alıp veritabanını koruyan kilitleme modeli. |
| **TTL Jitter (Avalanche Guard)** | Önbellek süresine rastgele $+0..180\text{s}$ ekleyerek tüm anahtarların aynı saniyede düşmesini engelleyen koruma deseni. |
| **Strategy & Adapter Pattern (LSP)** | `ICatalogCacheService` arkasında `MemoryCatalogCacheService` ve `RedisCatalogCacheService` stratejilerinin tak-çıkar uyumluluğu. |
| **Fail-Open Resilience Pattern** | Önbellek altyapısı çöktüğünde istemciye hata dönmeyip veritabanına şeffaf geri çekilme (graceful fallback). |

---

## 4. Yapılan Geliştirme Nasıl Test Edilir?

### 4.1 Birim Testleri (Unit Tests)
`ShuffleSeries.Catalog.Tests` projesi altında izole mock'lar ile test edilir:
```bash
dotnet test ShuffleSeries.Catalog/ShuffleSeries.Catalog.Tests/ShuffleSeries.Catalog.Tests.csproj --filter "RedisCatalogCacheServiceTests"
```
* **Test Case 1 (`CacheHit`):** Veri Redis'te varsa DB factory tetiklenmemeli, veri doğrudan JSON'dan deserialize edilip dönmeli.
* **Test Case 2 (`CacheMiss`):** Veri Redis'te yoksa DB factory tam 1 kez çalışmalı ve sonuç Redis'e deterministik TTL ile yazılmalı.
* **Test Case 3 (`CacheStampede`):** Aynı anahtara 5 eşzamanlı istek geldiğinde DB factory yalnızca 1 kez çalışmalı; diğer 4 istek kilidi bekleyip Redis'ten okumalı.
* **Test Case 4 (`FailOpen`):** Redis `RedisConnectionException` fırlattığında metot patlamamalı, DB factory'ye başvurup veriyi başarıyla dönmeli.
* **Test Case 5 (`RemoveAsync`):** Taksonomi mutasyonunda ilgili anahtar Redis'ten silinmeli.

### 4.2 Entegrasyon Testleri (Real Testcontainers PostgreSQL)
```bash
dotnet test ShuffleSeries.Catalog/ShuffleSeries.Catalog.IntegrationTests/ShuffleSeries.Catalog.IntegrationTests.csproj
```
* **Test Case:** 68 adet gerçek PostgreSQL entegrasyon testi koşulur. `CatalogApiFactory` dış bağımlılıkları izole ederek test süresini korur ve veritabanı işlemlerini doğrular.

### 4.3 Canlı Hazır Bulunuşluk ve Uçtan Uca Gateway Testleri (E2E)
Docker ortamında `catalog-api` ayağa kaldırıldığında:
```bash
# 1. Doğrudan Catalog Readiness Kontrolü:
curl -s http://localhost:5010/health/ready | jq .

# 2. Master Gateway E2E Test Suite:
python3 e2e_tests/run_all.py
```
* Beklenen Yanıt:
```json
{
  "status": "Healthy",
  "type": "Readiness",
  "checks": [
    {
      "name": "Redis",
      "status": "Healthy",
      "description": "Redis sağlıklı. Ping gecikmesi: 0.3ms"
    },
    {
      "name": "PostgreSQL",
      "status": "Healthy"
    },
    {
      "name": "masstransit-bus",
      "status": "Healthy"
    }
  ]
}
```
* `test_catalog_api_gateway.py` ve master E2E testleri 123/123 (%100) başarıyla tamamlanır.
