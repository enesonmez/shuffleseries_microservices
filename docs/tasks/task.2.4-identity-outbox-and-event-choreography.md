# Task 2.4: Outbox Pattern ve Identity Event Choreography

## Ne Yaptık?
Bu görev kapsamında, ShuffleSeries dağıtık mikroservis ekosisteminde veri tutarlılığını (Data Consistency) ve servisler arası asenkron iletişimi garanti altına alan **Transactional Outbox Pattern** ve **Event Choreography** mimarisi hayata geçirildi.

Identity bounded context'inde gerçekleşen kritik yaşam döngüsü olayları (kullanıcı kaydı, misafir oturumunun kalıcı hesaba bağlanması ve hesap silme), doğrudan mesaj kuyruğuna yazılmak yerine PostgreSQL veritabanı transaction'ı içerisinde atomik olarak `OutboxMessages` tablosuna yazıldı ve arka plan işi (`ProcessOutboxMessagesJob`) aracılığıyla RabbitMQ'ya aktarıldı.

Gerçekleştirilen temel bileşenler ve geliştirmeler:
1. **Public Integration Event Sözleşmeleri (`ShuffleSeries.Shared.Core.Domain.Events`):**
   * Diğer mikroservislerin (`Notification`, `TicketEconomy`, `Profile`, `UserLibrary`, `History`) tüketebileceği, servis bağımsız sözleşmeler tanımlandı:
     * `UserRegisteredEvent : IIntegrationEvent` (ve `IUserRegisteredEvent` sözleşmesi): Yeni kullanıcı (veya misafir) kaydolduğunda fırlatılır.
     * `UserMergedEvent : IIntegrationEvent` (ve `IUserMergedEvent` sözleşmesi): Misafir oturumu kalıcı hesaba bağlandığında fırlatılır.
     * `UserAccountDeletedEvent : IIntegrationEvent` (ve `IUserAccountDeletedEvent` sözleşmesi): Kullanıcı hesabı silindiğinde fırlatılır.
   * Tüm entegrasyon event'lerinin `IIntegrationEvent` uyguladığı ve `sealed record` olduğu mimari testlerle (`SharedCoreArchitectureTests`) garantiye alındı.

2. **Domain Event -> Integration Event Çevirimi (Anti-Corruption Layer - ACL):**
   * Identity Bounded Context'inin iç iş kurallarını temsil eden Domain Event'leri (`UserRegisteredDomainEvent`, `UserMergedDomainEvent`, `UserAccountDeletedDomainEvent`), dış dünyaya doğrudan sızdırılmayıp (leaking domain model anti-pattern'i önlenerek) Outbox Processor katmanında kamuya açık Integration Event'lere haritalandı.

3. **InBox Idempotency ve Deterministik Mesaj Eşleşmesi:**
   * Outbox tablosundaki mesajın `Id` (GUID) değeri, üretilen Integration Event nesnesinin `Id` alanına birebir atandı.
   * Bu sayede downstream tüketiciler (`Notification`, `TicketEconomy` vb.), ağ gecikmesi veya tekrar denemelerinde (retries) aynı mesajı aldıklarında `event.Id` üzerinden kendi `InboxMessages` tablolarında mükerrerliği (duplicate processing) anında engelleyebilir hale getirildi.

4. **Poison Message Koruması ve Dayanıklı Arka Plan İşi (`ProcessOutboxMessagesJob`):**
   * Quartz tabanlı Outbox işleyicisine zehirli mesaj (poison message) savunması eklendi:
     * Hata durumunda `RetryCount` artırılır ve hata detayı (`Error`) güncellenir.
     * Maksimum yeniden deneme eşiği (`MaxRetries = 3`) aşıldığında mesaj `ProcessedOnUtc = DateTime.UtcNow` yapılarak döngüden çıkarılır; sistemin sonsuz hata döngüsüne girmesi ve kuyruğu tıkaması (head-of-line blocking) engellenir.
     * Yapılandırılmış loglama (`ILogger<ProcessOutboxMessagesJob>`) ile tam gözlemlenebilirlik sağlandı.

5. **Generic ve Yüksek Performanslı Tip Çözümleme (`DomainEventTypeCache`):**
   * Magic string ve pahalı `Type.GetType(string)` reflection aramaları kaldırılarak `ShuffleSeries.Shared.Core.Infrastructure.Outbox.DomainEventTypeCache` altyapısı kuruldu. Bounded context assembly'si (`typeof(Event).Assembly`) üzerinden `IDomainEvent` tipleri tek seferlik taranarak `FrozenDictionary<string, Type>` içine alındı; $O(1)$ sürede hem kısa isim (`UserRegisteredDomainEvent`) hem de tam isim (`FullName`) ile derleme zamanı tip güvenliği ve sıfır-tahsisatlı (zero-allocation) tip çözümleme sağlandı.

6. **Kapsamlı Test Piramidi (Unit, Architecture, Integration, E2E):**
   * **Unit Tests:** `ProcessOutboxMessagesJobTests` sınıfında 8 kapsamlı birim test; `DomainEventTypeCacheTests` sınıfında 7 birim test yazıldı (Başarılı eşlemeler, bilinmeyen event tipleri, bozuk JSON içerikleri, MassTransit broker çökmeleri ve poison message eşiği).
   * **Domain Unit Tests:** `UserTests` sınıfında `CreateSocial` ve `MergeGuest` domain event fırlatma mantığı test edildi.
   * **Architecture Tests:** `SharedCoreArchitectureTests` altında `IntegrationEvents_In_SharedCoreDomain_ShouldImplement_IIntegrationEvent_AndBeSealed` kuralı yazıldı ve doğrulandı.
   * **Real Integration Tests (Testcontainers + PostgreSQL):** `AuthEndpointsIntegrationTests` sınıfına gerçek PostgreSQL ve Outbox tabloları üzerinden `CreateGuestSession` ve `MergeGuest` senaryoları eklendi.
   * **E2E & HTTP İstekleri:** `ShuffleSeries.Identity.Api.http` dosyasına Sosyal Giriş (Google/Apple) ve Token Reuse senaryoları eklendi; API Gateway üzerinden 35/35 tam başarılı curl testi koşturuldu.

---

## Neden Yaptık? (Mimari Kararlar)

### 1. Neden Doğrudan Mesaj Kuyruğuna (RabbitMQ) Yazmak Yerine Transactional Outbox Kullandık?
* **Problem (Dual-Write Krizine Karşı Çözüm):** Mikroservislerde bir veritabanı yazma işlemi (örneğin kullanıcı tablosuna `INSERT`) ve hemen ardından bir mesaj kuyruğuna yazma işlemi (`IBus.Publish`) yapıldığında, işlem atomik değildir ("Dual-Write Problem"). Eğer veritabanı işlemi başarılı olur ama mesaj fırlatılırken ağ koparsa veya broker yanıt vermezse, kullanıcı kaydedilir fakat downstream servisler (Hoş geldin maili, 14 günlük balayı biletleri) durumdan haberdar olamaz; sistem tutarsız duruma düşer.
* **Tersine Durum:** Eğer önce kuyruğa mesaj atılır sonra veritabanı işlemi hata verirse (örneğin DB unique constraint hatası), var olmayan bir kullanıcı için downstream servisler bilet oluşturmaya çalışır ("Ghost Entity").
* **Çözüm (Transactional Outbox):** Domain event'leri, `InsertOutboxMessagesInterceptor` sayesinde ana iş verisiyle aynı PostgreSQL veritabanı transaction'ında atomik olarak `OutboxMessages` tablosuna yazılır. Veritabanı transaction'ı commit olduğunda event'in kaydedildiği %100 kesindir. Bağımsız bir arka plan işi (`ProcessOutboxMessagesJob`) bu kayıtları güvenli şekilde RabbitMQ'ya aktarır.

### 2. Neden Domain Event'leri Doğrudan RabbitMQ'ya Yayınlamadık? (Domain vs. Integration Event İzolasyonu)
* **Bounded Context İzolasyonu (DDD Standartları):** Domain Event'leri (`UserRegisteredDomainEvent`), Identity Bounded Context'inin iç kurallarına, entity durumlarına ve yerel terminolojisine sıkı sıkıya bağlıdır.
* Eğer downstream servisler (`NotificationService`, `TicketEconomyService`) doğrudan `Identity.Domain.Events` kütüphanesini referans alırsa:
  1. Katmanlar arası yüksek bağımlılık (tight-coupling) oluşur.
  2. Bounded Context sınırları ihlal edilir.
  3. Identity içindeki bir entity refactoring'i tüm mikroservisleri derlenemez hale getirir (breaking change).
* **Anti-Corruption Layer (ACL):** Sektör standardı olarak Domain Event'leri içerde kalır; Outbox Processor bir çevirici (adapter/ACL) görevi üstlenerek kamuya açık, kararlı ve geriye dönük uyumlu `ShuffleSeries.Shared.Core.Domain.Events` sözleşmelerine dönüştürür.

### 3. Neden Outbox Message Id'si ile Integration Event Id'si 1-e-1 Eşleştirildi?
* **InBox Idempotency Güvencesi:** Dağıtık ağlarda "At-least-once Delivery" (en az bir kez teslim) kuralı geçerlidir; ağ dalgalanmaları veya servis yeniden başlatmalarında aynı mesaj tüketiciye birden fazla kez iletilebilir.
* Eğer Outbox Processor her denemede yeni bir rastgele `Guid.NewGuid()` üretirse, tüketici tarafındaki InBox filtresi aynı eylemi iki farklı event sanarak iki kere işler (örneğin kullanıcıya iki kez hoş geldin maili gider veya biletler mükerrer tanımlanır).
* `outboxMessage.Id` değerini Integration Event sözleşmesinin `Id` alanına aktararak tüketicinin aynı mesajı tekilleştirebilmesi (deduplication) garanti altına alındı.

### 4. Neden Poison Message Eşiği (`MaxRetries = 3`) Tanımlandı?
* Eğer Outbox tablosundaki bir kayıt (örneğin bozuk JSON veya geçersiz karakterler nedeniyle) deserialization aşamasında kalıcı bir hata verirse ve bu hata yönetilmezse:
  * Her job çalıştığında aynı ilk 20 mesaj çekilir.
  * Hatalı mesaj her defasında exception fırlatır.
  * Hata nedeniyle o mesaj işlenmiş (`ProcessedOnUtc`) olarak işaretlenemez.
  * Sonuç olarak sonraki tüm geçerli Outbox mesajları bu hatalı mesajın arkasında kuyrukta takılı kalır (**Head-of-Line Blocking**).
* `RetryCount >= MaxRetries (3)` kontrolü ile zehirli mesaj loglanır ve `ProcessedOnUtc` atanarak kuyruğun akışı korunur.

---

## Hangi Pattern'leri Kullandık?

```
┌────────────────────────────────────────────────────────────────────────────────────────┐
│                        SHUFFLESERIES IDENTITY EVENT CHOREOGRAPHY                       │
└────────────────────────────────────────────────────────────────────────────────────────┘

 [ Client Request ]
         │
         ▼
 ┌──────────────────────┐
 │  Identity Endpoint   │
 └──────────┬───────────┘
            │
            ▼
 ┌───────────────────────────────────────────────────────────────┐
 │ Identity Domain & Application Layer                           │
 │                                                               │
 │  user.Register() / user.MergeGuest() / user.Delete()          │
 │         │                                                     │
 │         ▼                                                     │
 │  RaiseDomainEvent(DomainEvent)                                │
 └──────────┬────────────────────────────────────────────────────┘
            │
            ▼
 ┌───────────────────────────────────────────────────────────────┐
 │ Unit of Work (PostgreSQL Transaction)                         │
 │                                                               │
 │  ┌─────────────────────────┐   ┌───────────────────────────┐  │
 │  │      Users Table        │   │   OutboxMessages Table    │  │
 │  │   (Insert / Update)     │   │     (Insert DomainEvent)  │  │
 │  └─────────────────────────┘   └───────────────────────────┘  │
 └──────────┬────────────────────────────────────────────────────┘
            │  (Atomik Commit - Dual-Write Önleme)
            ▼
 ┌───────────────────────────────────────────────────────────────┐
 │ Outbox Processor (Quartz Background Job)                      │
 │                                                               │
 │  1. Polling: OutboxMessages WHERE ProcessedOnUtc IS NULL      │
 │  2. Anti-Corruption Layer (ACL):                              │
 │     DomainEvent  ───►  Shared.Core IntegrationEvent           │
 │     outbox.Id    ───►  integrationEvent.Id                   │
 │  3. Poison Defense: MaxRetries = 3                            │
 └──────────┬────────────────────────────────────────────────────┘
            │
            ▼ MassTransit IPublishEndpoint
 ┌──────────────────────┐
 │ RabbitMQ Topic / Fan │
 └──────────┬───────────┘
            │ Event Choreography
    ┌───────┼──────────────────────────────┐
    ▼       ▼                              ▼
┌────────┐ ┌───────────────┐        ┌───────────────┐
│Profile │ │Ticket Economy │        │ Notification  │
│Service │ │   Service     │        │    Service    │
└────────┘ └───────────────┘        └───────────────┘
```

1. **Transactional Outbox Pattern:**
   * Veritabanı yazma operasyonu ile mesaj fırlatma operasyonunu aynı ACID transaction içinde atomik kılarak veri kaybı ve hayali event riskini sıfırlar.
2. **Event Choreography (Koreografi Modeli):**
   * Mikroservislerin merkezi bir orchestrator yerine birbirlerinin public integration event'lerini dinleyerek kendi iç iş mantıklarını yürüttüğü gevşek bağlı (loosely-coupled) mimari desen.
3. **Anti-Corruption Layer (ACL):**
   * Domain modellerinin dış servis sözleşmelerine sızmasını engelleyen, modelleri kamuya açık ortak sözleşmelere dönüştüren katman.
4. **Idempotent Consumer (InBox Pattern Desteği):**
   * Aynı mesajın ağ tekrarları sonucunda birden fazla kez işlenmesini önlemek amacıyla benzersiz event `Id`'sinin tüketici tarafında saklanmasını sağlayan anahtar yapısı.
5. **Dead-Letter / Poison Message Quarantine:**
   * Sürekli başarısız olan mesajların tüm kuyruğu tıkamasını önleyip belirli deneme eşiğinden sonra karantinaya alınması deseni.

---

## Yapılan Geliştirme Nasıl Test Edilir?

### 1. Test Piramidi Doğrulaması (358 Test)
Tüm çözümdeki testleri çalıştırmak için:
```bash
dotnet test ShuffleSeries.sln
```
**Sonuçlar:**
* `ShuffleSeries.Shared.Core.Tests`: 188 test passed
* `ShuffleSeries.Catalog.Tests`: 58 test passed
* `ShuffleSeries.Catalog.IntegrationTests`: 18 test passed
* `ShuffleSeries.ArchitectureTests`: 31 test passed
* `ShuffleSeries.ApiGateway.IntegrationTests`: 2 test passed
* `ShuffleSeries.Identity.Tests`: 53 test passed
* `ShuffleSeries.Identity.IntegrationTests`: 8 test passed
* **Toplam: 358 test passed (0 failed, 0 skipped)**

### 2. Mimari Kural Testi (NetArchTest)
Tüm `Shared.Core.Domain.Events` sınıflarının kurallara uygunluğunu doğrular:
```bash
dotnet test ShuffleSeries.ArchitectureTests --filter "IntegrationEvents_In_SharedCoreDomain_ShouldImplement_IIntegrationEvent_AndBeSealed"
```

### 3. Gerçek PostgreSQL + Testcontainers Entegrasyon Testleri
```bash
dotnet test ShuffleSeries.Identity/ShuffleSeries.Identity.IntegrationTests/ShuffleSeries.Identity.IntegrationTests.csproj
```
Doğrulanan senaryolar:
* `Register_WithValidPayload_ShouldReturn200_AndPersistUserWithRolesAndOutbox`
* `CreateGuestSession_ShouldReturnGuestToken_WithGuestClaims_AndPersistOutbox`
* `MergeGuest_WithValidPayload_ShouldReturn204_AndPersistUserMergedDomainEvent`
* `DeleteAccount_WithValidBearerToken_ShouldSoftDelete_AndWriteOutboxEvent`

### 4. Uçtan Uca (E2E) Canlı Sistem Test Paketi
API Gateway (`http://localhost:5001`) üzerinden koşturulan kapsamlı curl test paketi:
```bash
bash scratch/curl_test_auth_suite.sh http://localhost:5001
```
**Sonuç:**
```
===========================================================================
📊 TEST RESULTS: Passed: 35 / Total: 35
===========================================================================
```

### 5. .http Dosyası ile Manuel Doğrulama
IDE (Rider / VS / VS Code REST Client) üzerinden `ShuffleSeries.Identity/ShuffleSeries.Identity.Api/ShuffleSeries.Identity.Api.http` dosyası açılarak:
* 5.1 Misafir Oturumu oluşturma
* 6.1 Misafir Hesabını Birleştirme (`POST /api/auth/merge-guest`)
* 9.2 Hesap Silme (`DELETE /api/auth/account`)
* 11.1 ve 11.2 Sosyal Giriş (`POST /api/auth/social-login`)
senaryoları bağımsız olarak tetiklenebilir.
