---
trigger: always_on
description: Enterprise High-Performance & Distributed Scalability Guard - Zero N+1 Loop I/O, Cartesian Explosion (.AsSplitQuery), Zero-Allocation, Redis TTL/Stampede, RabbitMQ Outbox/InBox, ThreadPool sync-over-async defense.
---

## Performance & Scalability Guard (Demir Kural - Sıfır Performans Borcu)

Adopt the mindset of a High-Scale Distributed Systems Architect: Write code that never chokes under millions of requests, eliminates loop-level I/O, and protects memory/CPU boundaries before code hits production.

### Core Non-Negotiable Performance Mandates

1. **Sıfır Döngü-İçi Veritabanı Sorgusu (Zero Loop I/O / N+1 Yasağı - Demir Kural):**
   - Döngüler (`foreach`, `for`) içinde ASLA `await repository.GetByIdAsync(...)`, `_context.Set<T>().FindAsync(...)` veya LINQ çağrılamaz.
   - İhtiyaç duyulan tüm veriler döngü öncesinde tek bir toplu sorguyla (`GetByIdsAsync`, `GetActive*`) çekilip `Dictionary<TKey, TValue>` veya `HashSet<TKey>` olarak belleğe alınır; döngü içi tüm kontroller $O(1)$ In-Memory çalışır.

2. **Kartezyen Çarpım Koruması (Cartesian Explosion Guard - .AsSplitQuery):**
   - Birden fazla 1-N koleksiyon içeren eager-load (`Include` + `ThenInclude`) sorgularında veritabanı satır sayısının geometrik patlamasını önlemek için `.AsSplitQuery()` zorunludur.

3. **Salt Okuma İzolasyonu (.AsNoTracking) & Projeksiyon:**
   - CQRS Query handler'larında ve salt okuma listelemelerinde ChangeTracker maliyetini sıfırlamak için `.AsNoTracking()` zorunludur.
   - Tüm entity'yi çekmek yerine DTO seviyesinde `.Select(x => new Dto(...))` projeksiyonu uygulanmalıdır.

4. **Yabancı Anahtar Ters İndeks ve Soft-Delete Kısmi İndeks:**
   - Çoka-çok junction tablolarında (`MediaPlatforms`, `UserRoles`) ikinci FK kolonuna (`builder.HasIndex(x => x.PlatformId)`) açık B-Tree indeks eklenmelidir.
   - Soft-delete uygulanan tablolardaki tekil indeksler mutlaka `HasFilter("\"IsDeleted\" = false")` ile filtrelenmelidir.

5. **Toplu Veri Değişikliklerinde Bellek Koruması (.ExecuteDelete / .ExecuteUpdate):**
   - Yüzlerce satırı silmek veya güncellemek için belleğe entity yüklenip `RemoveRange` çağrılamaz; doğrudan SQL üreten `.ExecuteDeleteAsync()` / `.ExecuteUpdateAsync()` kullanılmalıdır.

6. **Dağıtık Önbellek (Redis) Güvenliği:**
   - Redis'e yazılan her dinamik anahtarın (`SET`) mutlaka deterministik bir TTL'i olmalıdır; TTL'siz dinamik anahtar üretimi yasaktır.
   - Cache Stampede'e karşı kilitli okuma (`SemaphoreSlim` / Cache-Aside with Lock); Cache Avalanche'e karşı rastgele TTL Jitter ($+ \text{rand}(0, 300\text{s})$) kullanılmalıdır.
   - Döngü içinde tek tek Redis çağrısı yapılamaz; `MGET`, Pipeline veya Lua script kullanılmalıdır.

7. **Asenkron Kuyruk (RabbitMQ) Bütünlüğü:**
   - Çift yazma (Dual-write) hatasına karşı daima **Transactional Outbox Pattern** kullanılmalıdır.
   - Tüketici (Consumer) tarafında **InBox Pattern** veya `IdempotencyKey` doğrulanmadan hiçbir iş kuralı yürütülemez (At-Least-Once Delivery savunması).
   - Zehirli mesajlara (Poison message) karşı `MaxRetries = 3` ve Dead Letter Queue (DLQ) / Outbox karantinası zorunludur.
   - Consumer RAM patlamasına karşı `PrefetchCount = 16 - 64` sınırlandırılmalıdır.

8. **.NET 10 CLR & ThreadPool Güvenliği:**
   - Asenkron kodlarda `.Result`, `.Wait()` veya `Task.WaitAll()` ile senkron bloklama (`sync-over-async`) KESİNLİKLE YASAKLANMIŞTIR (ThreadPool Starvation & Deadlock koruması).
   - Yüksek frekanslı bellek tamponlarında LOH ve GC Gen0/1 baskısını önlemek için `ArrayPool<T>.Shared`, `Memory<T>` / `ReadOnlySpan<T>` kullanılmalıdır.
   - Sık okunan değişmez sözlüklerde .NET 8/10 `FrozenDictionary` ve `FrozenSet` ($O(1)$ zero-allocation) tercih edilmelidir.
   - Tüm asenkron metotlar ve EF Core/Redis çağrıları parametre olarak gelen `CancellationToken`'ı tüketmelidir.
   - Regex desenleri ReDoS'a karşı daima `[GeneratedRegex]` ve `matchTimeoutMilliseconds: 250` ile tanımlanmalıdır.

9. **Ağ & API DoS Koruması:**
   - Sınırsız sayfalama yapılamaz; `MaxPageSize = 100` tavanı uygulanmalıdır.
   - Soket tükenmesine (Socket Exhaustion) karşı daima `IHttpClientFactory` kullanılmalıdır; doğrudan `new HttpClient()` açılamaz.
