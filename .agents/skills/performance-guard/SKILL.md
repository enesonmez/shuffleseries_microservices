---
name: performance-guard
description: Enterprise High-Performance, Database (EF Core / PostgreSQL), In-Memory Caching (Redis), Message Broker (RabbitMQ) and .NET Runtime Scalability Guard.
---

# Performance & Distributed Scalability Guard

Senior Dağıtık Sistemler ve Mikroservis Mimarı bakış açısıyla; PostgreSQL/EF Core, Redis, RabbitMQ, .NET 10 CLR ve ağ katmanlarında en sık karşılaşılan performans felaketlerini (anti-patterns) kod yazılmadan önce engelleyen kurallar bütünüdür.

---

## 1. Veritabanı & ORM (PostgreSQL & EF Core) Muhafızı

### 1.1 Sıfır Döngü-İçi Veritabanı Sorgusu (Zero Loop I/O - Kesin N+1 Yasağı)
- **Tuzak:** Döngü (`foreach`, `for`) içinde `await repository.GetByIdAsync(...)`, `_context.Set<T>().FindAsync(...)` veya LINQ sorgusu çağırmak.
- **Kural:** İhtiyaç duyulan veriler döngüye girmeden önce **tek bir toplu sorguyla** (`GetByIdsAsync(ids)` veya `GetActive*()`) çekilir; belleğe `Dictionary<TKey, TValue>` veya `HashSet<TKey>` olarak alınır. Döngü içindeki tüm eşleştirme ve doğrulamalar $O(1)$ RAM üzerinden yürütülür.
```csharp
// ❌ YANLIŞ: Döngü içinde N+1 veritabanı sorgusu
foreach (var id in request.PlatformIds) {
    var platform = await _repo.GetByIdAsync(id) ?? throw new NotFoundException();
    entity.AddPlatform(platform.Id);
}

// ✅ DOĞRU: Tek sorgu + In-Memory $O(1)$ Hash Table araması
var activePlatforms = (await _repo.GetActivePlatformsAsync(cancellationToken)).ToDictionary(p => p.Id);
foreach (var id in distinctIds) {
    if (!activePlatforms.TryGetValue(id, out var platform))
        throw new PlatformNotFoundException(id);
    entity.AddPlatform(platform.Id);
}
```

### 1.2 Kartezyen Çarpım Koruması (Cartesian Explosion Guard)
- **Tuzak:** Bir ana varlığa birden fazla 1-N ilişkili child koleksiyon (`Include` + `ThenInclude`) aynı SQL sorgusunda yüklendiğinde satır sayısı geometrik olarak katlanır ($N \times M \times K$).
- **Kural:** Birden fazla child koleksiyonun aynı anda eager-load edildiği tüm sorgularda açıkça `.AsSplitQuery()` çağrılmalıdır.
```csharp
// ✅ DOĞRU: Split Query ile izole ve optimize SQL sorguları
var series = await _context.Series
    .Include(s => s.Translations)
    .Include(s => s.Seasons).ThenInclude(sn => sn.Episodes)
    .Include(s => s.Platforms)
    .AsSplitQuery()
    .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
```

### 1.3 Salt Okuma İzolasyonu (.AsNoTracking)
- **Tuzak:** CQRS Query handler'larında ve salt okunacak listeleme işlemlerinde `AsNoTracking()` kullanılmaması. EF Core yüzlerce entity'nin bellek snapshot'ını ChangeTracker'da tutar; SaveChanges çağrılmasa dahi bellek şişer ve GC baskısı artar.
- **Kural:** Salt okuma yapan tüm repository sorgularında `AsNoTracking()` zorunludur.

### 1.4 Aşırı Veri Çekme (Over-Fetching) & Eksik Projeksiyonlar
- **Tuzak:** Sadece 2-3 sütuna (örn. `Id`, `Title`) ihtiyaç varken tüm entity'yi (2000 karakterlik `Overview`, `JsonData`, audit alanları) `SELECT *` ile çekmek.
- **Kural:** Listeleme ve özet sorgularında doğrudan DTO'ya `.Select(x => new MyDto(x.Id, x.Title))` projeksiyonu yapılmalıdır.

### 1.5 Yabancı Anahtarlarda (FK) Ters İndeks Kuralı
- **Tuzak:** PostgreSQL'de Foreign Key alanlarına otomatik B-Tree indeks açılmaz. Çoka-çok junction tablolarında (`MediaPlatforms`, `UserRoles`) bileşik primary key `(MediaId, PlatformId)` ilk sütunu indekslerken, ikinci sütun (`PlatformId`) üzerinden yapılan ters sorgular Full Table Scan yapar ve Cascade DELETE anında tüm tabloyu kilitler (`Table Lock`).
- **Kural:** Junction tablolarında ikinci FK sütununa (`builder.HasIndex(x => x.PlatformId)`) mutlaka açık B-Tree indeks eklenmelidir.

### 1.6 Soft Delete ve Kısmi İndeks (PostgreSQL Partial Index)
- **Tuzak:** Soft-delete uygulanan tablolarda `Title`, `Slug`, `TmdbId`, `ImdbId` alanlarına unconditional `UNIQUE` indeks koymak; silinmiş kayıtların aynı değerle yeni kayıt eklenmesini bloke etmesi.
- **Kural:** Soft-delete içeren tablolardaki tekil indeksler mutlaka `HasFilter("\"IsDeleted\" = false")` ile filtrelenmelidir.

### 1.7 Toplu İşlemlerde Bellek Koruması (.ExecuteDeleteAsync / .ExecuteUpdateAsync)
- **Tuzak:** Binlerce satırı silmek veya güncellemek için veritabanından ChangeTracker'a entity yükleyip `RemoveRange` çağırmak (OOM ve CPU spike).
- **Kural:** Toplu silme (örn. süresi dolmuş refresh token'lar) ve toplu güncellemelerde doğrudan SQL üreten `.ExecuteDeleteAsync()` ve `.ExecuteUpdateAsync()` kullanılmalıdır.

---

## 2. Dağıtık Önbellek & In-Memory (Redis) Muhafızı

### 2.1 Önbellek İzdihamı (Cache Stampede / Thundering Herd)
- **Tuzak:** Çok popüler bir anahtarın (Hot Key) süresi dolduğu anda gelen binlerce eşzamanlı isteğin aynı anda veritabanına hücum ederek DB'yi çökertmesi.
- **Kural:** Read-Through önbellek çözümlerinde `SemaphoreSlim(1, 1)` ile kilitli okuma (Cache-Aside with Lock) veya Probabilistic Early Expiration (XFetch) kullanılmalıdır.

### 2.2 Önbellek Çığı (Cache Avalanche) & TTL Jitter
- **Tuzak:** Servis ayağa kalktığında tüm anahtarlara aynı sabit sürenin (örn. tam 60 dakika) verilmesi ve 60. dakikada hepsinin aynı anda düşerek DB'yi boğması.
- **Kural:** Tüm cache TTL sürelerine rastgele bir sapma payı (Jitter: $+ \text{Random}(0, 300\text{ sn})$) eklenmelidir.

### 2.3 Boş Sorgu Sızması (Cache Penetration)
- **Tuzak:** Sistemde var olmayan ID'lerin (örn. brute-force / bot sorguları) sürekli cache'i ıskalayarak doğrudan veritabanına vurması.
- **Kural:** Veritabanında bulunamayan kayıtlar için kısa ömürlü (1-2 dakika) "boş/null" kayıt önbelleğe alınmalı veya Bloom Filter kullanılmalıdır.

### 2.4 Büyük Anahtar (Big Key) & N+1 Redis Roundtrip Yasağı
- **Tuzak:** Redis tek iş parçacıklı (single-threaded) bir motordur. Tek bir key içine megabaytlarca veri koymak veya döngü içinde 50 kez ayrı ayrı `GET/SET` çağırmak TCP gecikmesini katlar.
- **Kural:** Büyük listeler parçalanmalı (`HSCAN`/`SSCAN`); çoklu okuma/yazmalarda `MGET`, `Pipeline` veya Lua script kullanılmalıdır. Tüm dinamik anahtarlara mutlaka **deterministik bir TTL** atanmalıdır (Sonsuz anahtar yasaktır).

---

## 3. Asenkron Kuyruk & Mesajlaşma (RabbitMQ / MassTransit) Muhafızı

### 3.1 Çift Yazma Hatası (Dual-Write Prevention)
- **Tuzak:** Veritabanına `SaveChangesAsync` yaptıktan sonra doğrudan message broker'a publish çağrısı yapmak. Ağ kesintisinde veritabanı commit edilir ancak event kaybolur; mikroservisler arası tutarsızlık oluşur.
- **Kural:** Her zaman **Transactional Outbox Pattern** kullanılmalıdır. Domain event'ler entity mutasyonuyla aynı ACID transaction'da `OutboxMessages` tablosuna yazılır.

### 3.2 At-Least-Once Delivery & Idempotent Tüketim (InBox Pattern)
- **Tuzak:** Broker ağ kesintisinde aynı mesajı tekrar iletebilir (`Redelivered`). Tüketici bu mesajı ikinci kez işlerse mükerrer bilet/para düşümü veya mükerrer işlem gerçekleşir.
- **Kural:** Tüketici (Consumer) tarafında `InBox Pattern` veya deterministik `IdempotencyKey` doğrulaması yapılmadan hiçbir yan etki (side-effect) işletilemez.

### 3.3 Zehirli Mesaj (Poison Message / Head-of-Line Blocking)
- **Tuzak:** Deserialization hatası veren veya işlenemeyen 1 bozuk mesaj yüzünden consumer'ın sürekli exception fırlatıp mesajı kuyruğun başına nack/reject yapması ve arkadaki binlerce mesajın bloke olması.
- **Kural:** Her arka plan işinde ve kuyruk tüketicisinde katı bir deneme tavanı (`MaxRetries = 3`) olmalı; eşik aşıldığında mesaj Dead Letter Queue (DLQ) veya Outbox Karantinasına alınmalıdır.

### 3.4 Sınırsız Prefetch (Unbounded Prefetch / Consumer OOM)
- **Tuzak:** Consumer `PrefetchCount` ayarının yapılmaması (varsayılan limitsiz). RabbitMQ kuyruktaki 50.000 mesajı bir anda consumer'ın belleğine pompalar ve pod OOMKilled olur.
- **Kural:** Consumer'larda `PrefetchCount = 16 - 64` aralığında katı sınırlandırılmalıdır.

---

## 4. .NET 10, C# 14 & CLR Bellek / CPU / Concurrency Muhafızı

### 4.1 Thread Pool Starvation & Sync-over-Async (Ölümcül Deadlock)
- **Tuzak:** Async metotları `.Result`, `.Wait()` veya `Task.WaitAll()` ile senkron çağırmak. ASP.NET Core ThreadPool thread'lerini tüketir, istek kuyrukları şişer ve tüm sunucu yanıt veremez hale gelir.
- **Kural:** Uçtan uca saf asenkron (`async / await`) kod yazılmalıdır. Asla `task.Result` veya `task.Wait()` kullanılamaz.

### 4.2 LOH (Large Object Heap) Parçalanması & Gen0/1 GC Baskısı
- **Tuzak:** 85.000 byte'tan büyük nesneler doğrudan LOH'a gider ve pahalı Gen2 GC tetikler. Döngülerde string concatenation (`+`) yapmak milyonlarca geçici string tahsis eder.
- **Kural:**
  - Yüksek frekanslı bellek tamponlarında `ArrayPool<T>.Shared` ve `Memory<T>` / `ReadOnlySpan<T>` kullanılmalıdır.
  - Metin operasyonlarında `StringBuilder`, `Span<char>` veya `string.Create` tercih edilmelidir.
  - Sık okunan değişmez sözlüklerde .NET 8/10 `FrozenDictionary` ve `FrozenSet` (Thread-safe $O(1)$ zero-allocation) kullanılmalıdır.

### 4.3 Boxing / Unboxing İsrafı
- **Tuzak:** Değer tiplerinin (int, enum, Guid, struct) `object` parametrelerine veya generic olmayan koleksiyonlara (`ArrayList`) geçilerek heap'e kopyalanması.
- **Kural:** Generic arayüzler (`IEquatable<T>`, strongly-typed generic MediatR / MassTransit contracts) kullanılmalıdır (`CA2263`).

### 4.4 Cancellation Token İhmali (Orphaned Processing)
- **Tuzak:** İstemci sekmesini kapattığında veya HTTP timeout olduğunda arkadaki DB sorgusunun ve CPU döngüsünün `CancellationToken` dinlemediği için sunucuyu gereksiz yere yormaya devam etmesi.
- **Kural:** Tüm asenkron metotlar ve EF Core/Redis çağrıları parametre olarak gelen `CancellationToken`'ı tüketmelidir.

### 4.5 Regex DoS (ReDoS / Catastrophic Backtracking)
- **Tuzak:** İç içe geçmiş jokerli regex ifadelerinin (`(a+)+$`) kötü niyetli girdilerde CPU'yu %100'e kilitlemesi.
- **Kural:** Regex'ler .NET Source-Generated Regex (`[GeneratedRegex(..., RegexOptions.None, matchTimeoutMilliseconds: 250)]`) standardında tanımlanmalı ve daima bir `matchTimeout` içermelidir.

---

## 5. Ağ & API Darboğazları (Edge & Network)

### 5.1 Soket Tükenmesi (Socket Exhaustion)
- **Tuzak:** Her HTTP çağrısında `using var client = new HttpClient()` oluşturmak; işletim sistemi TCP `TIME_WAIT` soketlerinin tükenmesi (`SocketException`).
- **Kural:** Daima `IHttpClientFactory` ve singleton/pooled HTTP client mimarisi kullanılmalıdır.

### 5.2 Zincirleme Çöküş (Cascading Failures) & Timeout
- **Tuzak:** Dış servislere yapılan HTTP çağrılarında timeout tanımlanmaması; yavaşlayan tek bir bağımlılığın tüm mikroservis thread'lerini rehin alması.
- **Kural:** Polly ile katı timeout (2-5 sn), Exponential Backoff Retry ve Circuit Breaker zorunludur.

### 5.3 Sınırsız Sayfalama (Pagination DoS)
- **Tuzak:** İstemcinin `?pageSize=1000000` gönderebilmesi ve sunucunun milyon satırı belleğe çekmeye çalışması.
- **Kural:** Sistem genelinde `MaxPageSize = 100` tavanı uygulanmalıdır (`PaginationRequest` Single Source of Truth).
