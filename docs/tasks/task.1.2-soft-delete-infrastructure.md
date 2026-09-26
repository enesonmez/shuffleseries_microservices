# Task 1.2: Soft Delete Altyapısı (Soft Delete Infrastructure)

Bu doküman, ShuffleSeries mikroservis mimarisinde verilerin fiziksel olarak silinmesini önleyen, veri bütünlüğü ve denetlenebilirlik (auditability) sağlayan **Soft Delete** altyapısının `ShuffleSeries.Shared.Core` ve `ShuffleSeries.Catalog` servislerindeki tasarımını, mimari kararlarını, uygulanan desenleri ve doğrulama senaryolarını detaylandırır.

---

## 1. Ne Yaptık?

1. **Domain Seviyesinde Soft Delete & Hard Delete Sözleşmesi (`ISoftDeletable`, `IHardDeletable` & `BaseEntity`):**
   - `ISoftDeletable` arayüzü mantıksal silme için tanımlandı (`IsDeleted`, `DeletedAtUtc`, `DeletedBy`, `SoftDelete()`, `UndoSoftDelete()`).
   - `IHardDeletable` arayüzü Interface Segregation Principle (ISP) uyarınca fiziksel silme niyet beyanı için ayrı bir sözleşme olarak tanımlandı (`IsHardDeleteRequested`, `HardDelete()`).
   - `BaseEntity<TId>` sınıfına `IsDeleted` (varsayılan: `false`) ve non-mapped `IsHardDeleteRequested` alanları eklendi; her iki arayüz implemente edildi.
   - Domain varlıklarının kendi iç kurallarıyla mantıksal silinmesini, geri alınabilmesini veya fiziksel silinmesini sağlayan metotlar sağlandı.
   - `Catalog.Domain.Entities.Series` sınıfının `Delete()` metodu, `SoftDelete()` çağrısını ve ardından `SeriesDeletedDomainEvent` fırlatılmasını sağlayacak şekilde güncellendi.

2. **EF Core Seviyesinde Otomatik Müdahale ve Hard Delete Bypass Mekanizması (`SoftDeleteInterceptor` & `HardDeleteScope`):**
   - `SaveChangesInterceptor` tabanlı `SoftDeleteInterceptor` geliştirildi.
   - Değişiklik takipçisinde (`ChangeTracker`) `EntityState.Deleted` durumuna geçen tüm `ISoftDeletable` varlıklar yakalanarak durumları `EntityState.Modified`'a çevrildi.
   - **Hard Delete Desteği 1 (`HardDeleteScope`):** `AsyncLocal<bool>` tabanlı ambient scope geliştirildi. `using (HardDeleteScope.Begin())` bloğu açıldığında interceptor devre dışı kalarak EF Core'un fiziksel `DELETE FROM` SQL'i çalıştırması sağlandı.
   - **Hard Delete Desteği 2 (Domain Marker `entity.HardDelete()`):** Belirli bir varlık `entity.HardDelete()` ile işaretlenmişse, interceptor o varlığı atlayarak (`continue`) sadece o varlığın fiziksel silinmesine izin verdi.
   - Standart akışta `IsDeleted = true` ve `DeletedAtUtc = DateTime.UtcNow` alanları otomatik doldurularak veritabanına fiziksel silme sorgusu gönderilmesi engellendi.

3. **Global Query Filter ile Otomatik Filtreleme (`ModelBuilderExtensions`):**
   - `ApplySoftDeleteQueryFilters` genişletme metodu (extension method) yazıldı.
   - DbContext model kurulumunda (`OnModelCreating`), `ISoftDeletable` uygulayan tüm kök varlık türlerine Expression Tree kullanılarak dinamik olarak `.HasQueryFilter(e => !e.IsDeleted)` kuralı uygulandı.
   - `IsHardDeleteRequested` alanı `modelBuilder.Entity(...).Ignore(...)` ve `[NotMapped]` ile DB kolonundan tamamen yalıtıldı.
   - İhtiyaç duyulduğunda (örn. sistem yöneticisi ekranları veya arşiv sorguları) `.IgnoreQueryFilters()` çağrısıyla silinmiş kayıtların da sorgulanabilmesi sağlandı.

4. **Veritabanı İndeks Optimizasyonu ve PostgreSQL Filtrelenmiş İndeks (Partial Index):**
   - `SeriesConfiguration` üzerinde `IsDeleted` alanına B-Tree indeksi eklendi (sorgu performansı için).
   - `Title` alanındaki tekillik (Unique) kısıtı, PostgreSQL'e özel filtrelenmiş indeks (`HasFilter("\"IsDeleted\" = false")`) haline getirildi. Böylece mantıksal olarak silinmiş bir yapımın başlığı ileride tekrar kullanıldığında veritabanı seviyesinde çakışma yaşanması önlendi.
   - EF Core Migration oluşturuldu (`AddSoftDeleteToSeries`).

5. **Entegrasyon ve Bağımlılık Enjeksiyonu (DI):**
   - `Shared.Core.Infrastructure` katmanına `AddSharedInfrastructure()` metodu eklenerek `SoftDeleteInterceptor` ve `InsertOutboxMessagesInterceptor` tek merkezden DI konteynerine kazandırıldı.
   - `CatalogDbContext` konfigürasyonuna hem `SoftDeleteInterceptor` hem de `InsertOutboxMessagesInterceptor` kaydedildi.

6. **Test Kapsamı:**
   - `Shared.Core.Tests` içerisine `BaseEntityTests` (default değerler, `SoftDelete`, `UndoSoftDelete`) ve `SoftDeleteInfrastructureTests` (InMemory DbContext üzerinde Interceptor, Global Query Filter ve IgnoreQueryFilters testleri) eklendi.
   - `Catalog.Tests` içerisine `CatalogDbContextSoftDeleteTests` ve `SeriesTests.Delete` senaryoları eklendi.

---

## 2. Neden Yaptık?

- **Veri Güvenliği ve Denetlenebilirlik (Auditability):** Dağıtık mikroservis sistemlerinde ve kullanıcı deneyiminde yanlışlıkla veya kötü niyetle silinen verilerin kurtarılabilmesi kritik bir gereksinimdir. Fiziksel silme (Hard Delete) geri döndürülemez veri kaybına ve ilişkisel referans bozulmalarına neden olur.
- **Analitik ve Dağıtık Tutarlılık:** Kullanıcı kütüphanesinden veya katalogdan bir içerik kaldırıldığında, geçmiş izleme telemetrisi (History & Analytics) ve event tüketicileri bu içeriğin referansına ihtiyaç duymaya devam edebilir. Soft delete sayesinde eski kayıtlara referans veren loglar yetim kalmaz (orphan records).
- **Geliştirici Hatalarını Önleme:** Her LINQ sorgusuna elle `where x.IsDeleted == false` yazmak, geliştiricinin filtreyi unutması durumunda canlı ortamda silinmiş verilerin kullanıcılara gösterilmesine yol açar. Global Query Filter bu riski sıfıra indirir.
- **Filtrelenmiş İndeks (PostgreSQL Partial Index) ile Performans ve İş Kuralı:** Eğer `Title` üzerindeki Unique indeks filtrelenmezse, "Breaking Bad" dizisi silindiğinde ileride yeniden aynı başlıkla bir içerik girilemez. `\"IsDeleted\" = false` filtresi ile sadece aktif kayıtlar arasında tekillik zorlanır.

---

## 3. Hangi Pattern'leri Kullandık?

- **Interceptor Pattern (AOP - Aspect-Oriented Programming):** `SoftDeleteInterceptor`, uygulama kodundan bağımsız olarak veritabanına giden `SaveChanges` işlemine araya girerek (interception) fiziksel silme eylemini mantıksal güncellemeye dönüştürür.
- **Global Query Filter Pattern:** Entity Framework Core'un dinamik lambda filtreleme yeteneğiyle tüm LINQ sorgularına şeffaf bir `WHERE IsDeleted = false` koşulu ekler.
- **Layered Supertype / Marker Interface Pattern:** `ISoftDeletable` ve `BaseEntity<TId>`, bu yeteneğe sahip tüm varlıklar için tip güvenli bir sözleşme ve ortak alan havuzu sunar.
- **Domain-Driven Design (DDD) Rich Domain Model:** Entity'nin silinmesi sadece bir DB operasyonu değil, domain kuralıdır. `Series.Delete()` metodu varlığın durumunu günceller ve `SeriesDeletedDomainEvent` fırlatarak asenkron Outbox koreografisini tetikler.

---

## 4. Yapılan Geliştirme Nasıl Test Edilir?

### Test Komutları
Tüm çözümü ve yeni eklenen soft delete testlerini çalıştırmak için:
```bash
dotnet test
```

### Test Senaryoları (Test Cases)

| Test Adı | Kapsam | Beklenen Davranış |
| :--- | :--- | :--- |
| `IsDeleted_ShouldBeFalse_ByDefault` | Unit (Domain) | Yeni oluşturulan bir varlıkta `IsDeleted` false, `DeletedAtUtc` ve `DeletedBy` null olmalıdır. |
| `SoftDelete_ShouldSetIsDeletedTrue_AndPopulateAuditFields` | Unit (Domain) | `SoftDelete("admin")` çağrıldığında `IsDeleted` true olmalı, `DeletedAtUtc` şimdiki zamana ve `DeletedBy` "admin"e eşitlenmelidir. |
| `UndoSoftDelete_ShouldResetIsDeleted_AndClearAuditFields` | Unit (Domain) | `UndoSoftDelete()` çağrıldığında `IsDeleted` false, denetim alanları null olmalıdır. |
| `Delete_Should_MarkAsSoftDeleted_AndRaiseDomainEvent` | Unit (Catalog Domain) | `Series.Delete("manager")` hem soft delete yapmalı hem de `SeriesDeletedDomainEvent` üretmelidir. |
| `Remove_Should_ConvertToSoftDelete_AndPopulateAuditFields` | Integration (Shared EF) | `context.Remove(entity)` çağrıldığında `SoftDeleteInterceptor` devreye girmeli, entity DB'de `IsDeleted = true` kalmalıdır. |
| `Query_Should_AutomaticallyFilterOutSoftDeletedEntities` | Integration (Shared EF) | Normal `ToListAsync()` sorgusunda silinmiş kayıtlar sonuç listesine gelmemelidir. |
| `Query_WithIgnoreQueryFilters_ShouldReturnSoftDeletedEntities` | Integration (Shared EF) | `.IgnoreQueryFilters()` kullanıldığında hem aktif hem silinmiş kayıtlar dönmelidir. |
| `HardDeletableEntity_ShouldBeDeletedPhysically` | Integration (Shared EF) | `ISoftDeletable` uygulamayan bir varlık silindiğinde normal fiziksel silme çalışmalıdır. |
| `GetByIdAsync_Should_ReturnNull_When_SeriesIsSoftDeleted` | Integration (Catalog Repo) | Soft delete edilmiş bir dizi ID ile sorgulandığında Repository `null` dönmelidir. |
| `GetPagedListAsync_Should_ExcludeSoftDeletedSeries` | Integration (Catalog Repo) | Sayfalama sorgusu (`GetPagedListAsync`) silinmiş dizileri `TotalCount` ve `Items` listesinden çıkarmalıdır. |
| `HardDeleteScope_Should_BypassSoftDelete_AndPhysicallyDeleteEntity` | Integration (Shared EF) | `HardDeleteScope.Begin()` kapsamında silinen entity DB'den fiziksel olarak silinmelidir. |
| `EntityHardDelete_Should_BypassSoftDelete_ForSingleEntity` | Integration (Shared EF) | `entity.HardDelete()` çağrılan varlık fiziksel silinirken, diğer varlık soft delete olmalıdır. |
| `HardDeleteScope_Should_PhysicallyDeleteSeriesFromDatabase` | Integration (Catalog) | `HardDeleteScope` içinde silinen Series entity'si PostgreSQL/DB'den tamamen silinmelidir. |
| `Series_HardDelete_Should_PhysicallyDeleteSpecificSeries` | Integration (Catalog) | `series.HardDelete()` çağrıldığında Repository üzerinden fiziksel silme gerçekleşmelidir. |
