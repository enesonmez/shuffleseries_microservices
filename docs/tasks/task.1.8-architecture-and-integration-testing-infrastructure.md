# Task 1.8: Mimari Bağımlılık ve Testcontainers Tabanlı Gerçek Entegrasyon Test Altyapısı

## 1. Ne Yaptık?
Bu görev kapsamında ShuffleSeries mikroservis ekosistemine iki kritik kurumsal test altyapısı kazandırılmıştır:

1. **Merkezi Mimari Testler (`ShuffleSeries.ArchitectureTests`):**
   - `NetArchTest.Rules` kütüphanesi kullanılarak Onion Architecture katman kuralları (Domain, Application, Infrastructure, Presentation/Api) ve CQRS/DDD tasarım prensipleri kod seviyesinde otomatik denetlenen xUnit testlerine dönüştürüldü.
   - Herhangi bir katmanın mimari sınırları ihlal etmesi (örneğin Domain'in Infrastructure'a veya Application'ın Api'ye bağımlı olması, Handler sınıflarının `sealed` olmaması veya DomainEvent'lerin `IDomainEvent` arayüzünü uygulamaması) durumunda derleme/CI kapısında anında hata veren **Fitness Function** mekanizması kuruldu.

2. **Testcontainers Tabanlı Gerçek Entegrasyon Testleri (`ShuffleSeries.Catalog.IntegrationTests`):**
   - EF Core `InMemory` sağlayıcısının eksikliklerini ve yanıltıcı test sonuçlarını ortadan kaldırmak için `Testcontainers for .NET` (`postgres:16-alpine`) entegre edildi.
   - `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory<Program>`) ile Minimal API endpoint'leri gerçek HTTP istemcisi (`HttpClient`), routing, middleware hattı ve dependency injection ile uçtan uca test edildi.
   - `Respawn` kütüphanesi ile test metotları arasında migration şeması silinmeden tüm tablolar milisaniyeler (300-400 ms) içerisinde `TRUNCATE` edilerek yüksek hızlı ve tamamen izole bir test döngüsü sağlandı.
   - Projenin `Program.cs` dosyası testlenebilirlik için `public partial class Program;` bildirimiyle genişletildi.
   - PostgreSQL `jsonb` sütun tipi ve EF Core SQL çevrim kısıtları (`ILIKE` / `LOWER()`) gerçek veritabanı üzerinde test edilerek üretim ortamında ortaya çıkabilecek kritik çalışma zamanı hataları erkenden yakalandı ve düzeltildi.

---

## 2. Neden Yaptık? (Mimari Kararlar ve Sektör Standardı)

### Neden Mock veya EF Core InMemory Entegrasyon Testi Değildir?
* **InMemory Veritabanı Yalanları:** EF Core'un `InMemory` sağlayıcısı ilişkisel bir veritabanı değildir. Foreign key kısıtlamalarını denetlemez, PostgreSQL'e özgü kısmi indeksleri (`HasFilter("\"IsDeleted\" = false")`) uygulamaz, sequence ve transaction rollback işlemlerini çalıştırmaz. Daha da önemlisi, `string.Equals(a, b, OrdinalIgnoreCase)` gibi LINQ ifadeleri InMemory'de C# objesi olarak başarıyla çalışırken, PostgreSQL sağlayıcısında (Npgsql) SQL'e çevrilemediği için `InvalidOperationException` fırlatır.
* **Mock Yanılgısı:** `IRepository` veya `IPublishEndpoint` mock'landığında, sistemin gerçek entegrasyonu değil, bizim yazdığımız varsayımlar test edilmiş olur.
* **Testcontainers Çözümü:** Gerçek `postgres:16-alpine` container'ı ayağa kaldırarak üretim ortamındaki PostgreSQL motorunun birebir aynısını test aşamasında çalıştırır.

### Neden Respawn?
* Testler arasında veritabanını temizlemek için her defasında `EnsureDeleted()` ve `Migrate()` çalıştırmak test süresini onlarca saniyeye çıkarır.
* `Respawn`, veritabanı şemasını ve migration geçmişini (`__EFMigrationsHistory`) koruyarak yabancı anahtar sıralamasına göre tabloları milisaniyeler içinde boşaltır. Böylece hem veri kirliliği (test pollution) önlenir hem de testler ışık hızında çalışır.

### Neden Mimari Bağımlılık Testleri (NetArchTest.Rules)?
* Büyük ve mikroservis mimarisine sahip projelerde zamanla kod çürümesi (code rot) başlar. Farklı geliştiriciler farkında olmadan katman sınırlarını ihlal eden using'ler veya uygunsuz sınıf tasarımları ekleyebilir.
* Mimari testler (**Fitness Functions**), mimari standartları yaşayan ve kodla korunan otomatik kalite kapılarına dönüştürür.

---

## 3. Hangi Pattern'leri Kullandık?

| Pattern / Yaklaşım | Açıklama ve Sistemdeki İşlevi |
|---|---|
| **Architectural Fitness Functions** | Mimari kuralların (Onion Architecture sınırları, immutability, adlandırma standartları) birim testleri gibi otomatik denetlenmesini sağlar. |
| **Subcutaneous / Component Testing** | Kullanıcı arayüzünün hemen altından (HTTP Minimal API endpoint seviyesinden) başlayarak veritabanına kadar tüm katmanları uçtan uca doğrular. |
| **Shared Container Fixture (`ICollectionFixture`)** | Test suite boyunca tek bir PostgreSQL container'ı paylaşarak donanım kaynaklarını ve test süresini optimize eder. |
| **Database Respawner Pattern** | Veritabanı şemasını düşürmeden testler arasında anlık veri temizliği yaparak test izolasyonunu (Idempotency) garanti eder. |
| **Rich Domain & CQRS Conventions** | Entity'lerin encapsulation'ını, Handler'ların `sealed` yapısını ve Validator'ların tek tip kurallarını güvenceye alır. |

---

## 4. Yapılan Geliştirme Nasıl Test Edilir?

### A. Testleri Çalıştırma
Projeler hem IDE üzerinden hem de terminalden tek komutla çalıştırılabilir:

```bash
# 1. Mimari Bağımlılık Testlerini Çalıştırma (150-200 ms)
DOTNET_CLI_HOME=/tmp dotnet test ShuffleSeries.ArchitectureTests/ShuffleSeries.ArchitectureTests.csproj

# 2. Testcontainers Entegrasyon Testlerini Çalıştırma (Gerçek Docker PostgreSQL)
DOTNET_CLI_HOME=/tmp DOCKER_HOST="unix:///Users/enesonmez/.docker/run/docker.sock" \
  dotnet test ShuffleSeries.Catalog/ShuffleSeries.Catalog.IntegrationTests/ShuffleSeries.Catalog.IntegrationTests.csproj

# 3. Tüm Çözüm Testlerini Çalıştırma (188 Test)
DOTNET_CLI_HOME=/tmp DOCKER_HOST="unix:///Users/enesonmez/.docker/run/docker.sock" dotnet test
```

### B. Uygulanan Test Case'leri

#### Mimari Testler (`ShuffleSeries.ArchitectureTests`):
1. `Domain_ShouldNotHaveDependencyOn_OtherProjects`: Domain katmanının Application, Infrastructure veya Api katmanlarına bağımlılığı olmadığını doğrular.
2. `Application_ShouldNotHaveDependencyOn_InfrastructureOrApi`: Dependency Inversion prensibi uyarınca Application katmanının alt/üst katmanlara bağımlı olmadığını doğrular.
3. `Infrastructure_ShouldNotHaveDependencyOn_Api`: Altyapı katmanının sunum katmanına referans içermediğini kontrol eder.
4. `Handlers_ShouldHaveNameEndingWith_Handler`: MediatR handler sınıflarının isimlendirme kuralını denetler.
5. `Handlers_ShouldBeSealed`: Handler sınıflarının `sealed` olduğunu doğrular.
6. `Validators_ShouldHaveNameEndingWith_Validator`: FluentValidation sınıflarının isimlendirme kuralını denetler.
7. `DomainEvents_ShouldImplement_IDomainEvent`: Domain event'lerinin işaretçi arayüzü uyguladığını doğrular.
8. `Entities_ShouldInheritFrom_BaseEntityOrAggregateRoot`: Domain varlıklarının temel sınıflardan türediğini kontrol eder.
9. `SharedCoreDomain_ShouldNotHaveDependencyOn_OtherSharedLayers`: Shared.Core.Domain bağımsızlığını doğrular.
10. `SharedCoreApplication_ShouldNotDependOn_InfrastructureOrWeb`: Shared.Core.Application bağımsızlığını doğrular.
11. `SharedCoreExceptions_ShouldNotHaveDependencyOn_ApplicationInfrastructureOrWeb`: Exceptions çekirdeğinin bağımsızlığını doğrular.

#### Entegrasyon Testleri (`ShuffleSeries.Catalog.IntegrationTests`):
1. `CreateSeries_WithValidPayload_ShouldPersistInRealPostgreSql_AndReturn201Created`:
   - `POST /api/catalog/series` endpoint'ine geçerli payload gönderildiğinde 201 Created ve Location header döner.
   - PostgreSQL veritabanında satırın gerçekten oluştuğu ve Outbox tablosuna `SeriesCreatedDomainEvent` JSON kaydının yazıldığı doğrulanır.
2. `CreateSeries_WithDuplicateTitle_ShouldReturn422UnprocessableEntity`:
   - Aynı başlıkla ikinci bir seri oluşturulmak istendiğinde `BusinessException` tetiklenir ve RFC 4918 422 Unprocessable Entity ProblemDetails döner.
3. `GetSeriesById_WhenSeriesExists_ShouldReturn200Ok_AndCorrectData`:
   - Var olan bir ID sorgulandığında 200 OK ve eşleşen DTO döner.
4. `GetSeriesById_WhenNotExists_ShouldReturn404NotFound`:
   - Rastgele bir GUID sorgulandığında 404 Not Found döner.
5. `DeleteSeries_ShouldSoftDeleteInPostgreSql_AndHideFromGetEndpoint`:
   - `DELETE /api/catalog/series/{id}` çağrıldığında 204 NoContent döner.
   - Normal GET endpoint'i kaydı bulamaz (Global Query Filter doğrulaması).
   - Veritabanına `IgnoreQueryFilters()` ile doğrudan bakıldığında `IsDeleted = true` ve `DeletedAtUtc` alanının dolu olduğu (SoftDeleteInterceptor doğrulaması) kanıtlanır.
6. `GetSeriesList_ShouldReturnPaginatedResults_FromRealPostgreSql`:
   - Gerçek veritabanındaki 3 kayıt arasından sayfalama parametreleri (`?page=1&pageSize=2`) ile 2 kayıt, doğru `TotalCount`, `TotalPages` ve `HasNextPage` bayrakları doğrulanır.
