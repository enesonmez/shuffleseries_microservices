# Görev 3.2.5: Episode Domain Events, Outbox Entegrasyonu ve Event Simetrisi

## 1. Ne Yaptık?
Bu görevde, `Episode` varlığının sistem genelindeki Domain Event ve Transactional Outbox mimarisine entegrasyonu tamamlanmış, varlık seviyesindeki event asimetrisi giderilerek `Media`, `Platform`, `Mood` ve `Genre` aggregate'leri ile tam simetrik hale getirilmiştir.

1. **`IHaveDomainEvents` Arayüzünün Tanımlanması:**
   - Event fırlatma kabiliyeti, `IAggregateRoot` arayüzünden ayrılarak bağımsız bir `IHaveDomainEvents` sözleşmesine dönüştürüldü (`IAggregateRoot : IHaveDomainEvents`).
   - `Episode : BaseEntity, IHaveDomainEvents` implementasyonu ile `Episode` yapay olarak aggregate root yapılmadan birinci sınıf domain event fırlatma yeteneğine kavuşturuldu.

2. **Transactional Outbox Interceptor Entegrasyonu:**
   - `InsertOutboxMessagesInterceptor` filtresi `Where(x => x.Entity is IHaveDomainEvents)` olarak genişletildi.
   - Böylece `Episode` üzerindeki işlemler (`Create`, `Update`, `Delete`, platform/çeviri ekleme-silme) tek bir veritabanı transaction'ında `OutboxMessages` tablosuna güvenle yazıldı.

3. **Event Granularity & Simetri Düzenlemesi:**
   - Aşırı granüler olan ve diğer varlıklarla asimetri yaratan `EpisodePlatformAddedDomainEvent`, `EpisodePlatformRemovedDomainEvent`, `EpisodeTranslationUpdatedDomainEvent`, `EpisodeTranslationRemovedDomainEvent` kaldırıldı.
   - Projenin geri kalanıyla tam uyumlu 3 temel yaşam döngüsü event'i bırakıldı:
     - `EpisodeCreatedDomainEvent` (SeriesId, SeasonId, EpisodeId, EpisodeNumber, IsStandalone)
     - `EpisodeUpdatedDomainEvent` (EpisodeId, EpisodeNumber, IsStandalone, RuntimeMinutes, ImdbRating)
     - `EpisodeDeletedDomainEvent` (EpisodeId)

4. **Event Deduplication Guard (`RaiseUpdatedEvent`):**
   - `Episode.cs` içine deduplication mekanizması eklendi:
     ```csharp
     private void RaiseUpdatedEvent()
     {
         if (GetDomainEvents().Any(e => e is EpisodeCreatedDomainEvent or EpisodeUpdatedDomainEvent or EpisodeDeletedDomainEvent))
             return;

         RaiseDomainEvent(new EpisodeUpdatedDomainEvent(Id, EpisodeNumber, IsStandalone, RuntimeMinutes, ImdbRating));
     }
     ```
   - `UpdateDetails`, `AddPlatform`, `RemovePlatform`, `AddOrUpdateTranslation` ve `RemoveTranslation` metotları bu gard üzerinden `RaiseUpdatedEvent` çağırarak aynı transaction'da mükerrer outbox mesajı üretilmesini engelledi.

---

## 2. Neden Yaptık?
1. **Asimetri ve Tüketici Karmaşasının Önlenmesi (Coarse-Grained vs Fine-Grained):**
   - Downstream servisler (özellikle "Shuffle Engine" - Bölüm Ruleti, Elasticsearch ve Redis önbellek katmanı), bir bölümün çevirisinin veya platform URL'sinin değiştiğini tek tek ayrı kuyruklardan dinlemek yerine bölümün güncellendiğini bilip in-memory havuzunu veya Redis önbelleğini tazelemeyi tercih eder.
   - `Media` aggregate'inde tek bir `MediaUpdatedDomainEvent` fırlatılırken `Episode` için 5 ayrı granüler event fırlatmak asimetri yaratır ve tüketici tarafta 5 ayrı RabbitMQ consumer'ı açtırma zorunluluğu doğururdu.

2. **Outbox Message Stampede Koruması (Deduplication):**
   - Bir admin işlemi veya veri tohumlama sırasında aynı bölüm üzerinde peş peşe hem detay güncellenip hem platform veya dil eklendiğinde, Outbox tablosuna 3-4 kez `EpisodeUpdatedDomainEvent` yazılması kuyruk ve tüketici üzerinde fırtına etkisi (message stampede) yaratırdı. `RaiseUpdatedEvent` gardı ile bu mükerrerlik tamamen sıfırlandı.

3. **Domain Saflığı ve Interface Segregation:**
   - `Episode` mantıksal olarak `Season` altında yaşayan ve bağımsız bir Aggregate Root olmayan bir varlıktır. Sırf outbox event'i fırlatabilmesi için onu `AggregateRoot` yapmak DDD sınırlarını bozar; `Series` aggregate'ini ChangeTracker'a çekip event fırlattırmak ise gereksiz satır kilitlerine ve bellek baskısına yol açardı. `IHaveDomainEvents` ile bu ikilem en zarif şekilde çözüldü.

---

## 3. Hangi Pattern'leri Kullandık?

| Tasarım Deseni | Katman / Konum | Amaç |
| :--- | :--- | :--- |
| **Interface Segregation Principle (ISP)** | `Shared.Core.Domain.Primitives` | Domain event kabiliyetini (`IHaveDomainEvents`) Aggregate Root kimliğinden ayırmak. |
| **Transactional Outbox Pattern** | `Shared.Core.Infrastructure.Interceptors` | EF Core `SaveChanges` anında domain event'leri atomik olarak `OutboxMessages` tablosuna serileştirmek (Dual-write savunması). |
| **Event Deduplication Guard** | `Episode.cs` (`RaiseUpdatedEvent`) | Aynı iş kapsamında mükerrer `Updated` event'lerinin fırlatılmasını engelleyerek tekillik sağlamak. |
| **Coarse-Grained Domain Events** | `Catalog.Domain.Events` | Tüketici servislerin tek bir sözleşme üzerinden varlık güncellemelerini reaktif işlemesini sağlamak. |

---

## 4. Nasıl Test Edilir ve Test Senaryoları

### 4.1. Birim Testleri (Unit Tests)
`ShuffleSeries.Catalog.Tests.Domain.Entities.EpisodeTests` altındaki test senaryoları:
- **`Episode_Operations_Should_RaiseAppropriateDomainEvents_WithDeduplication`**:
  - `UpdateDetails` çağrıldığında `EpisodeUpdatedDomainEvent` fırlatıldığı,
  - Peşinden `AddPlatform` ve `AddOrUpdateTranslation` çağrıldığında `RaiseUpdatedEvent` gardı sayesinde listenin hala tek bir `EpisodeUpdatedDomainEvent` içerdiği,
  - `ClearDomainEvents` sonrası tekil `RemovePlatform`, `RemoveTranslation` işlemlerinin `EpisodeUpdatedDomainEvent` fırlattığı,
  - `Delete` çağrıldığında ise `EpisodeDeletedDomainEvent` fırlatıldığı doğrulanır.

### 4.2. Gerçek Entegrasyon Testleri (Real Integration Tests - Testcontainers + Respawn)
`ShuffleSeries.Catalog.IntegrationTests.Endpoints.EpisodeEndpointsIntegrationTests`:
- **`EpisodeOperations_ShouldProduceOutboxMessagesInPostgreSql`**:
  - `POST /api/catalog/episodes` -> Gerçek PostgreSQL'de `EpisodeCreatedDomainEvent` outbox kaydı oluşur.
  - `PUT /api/catalog/episodes/{id}` -> `EpisodeUpdatedDomainEvent` outbox kaydı oluşur.
  - `DELETE /api/catalog/episodes/{id}` -> `EpisodeDeletedDomainEvent` outbox kaydı oluşur.

### 4.3. Gateway End-to-End (E2E) Testleri
Terminal üzerinden:
```bash
python3 e2e_tests/run_all.py
```
- API Gateway üzerinden geçen tüm güvenlik, RBAC, IDOR, SQLi ve Catalog Episode endpoint senaryoları (%100 - 113/113) başarıyla doğrulanır.
