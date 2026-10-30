# Task 3.2: Catalog CQRS ve Minimal API Geliştirmeleri (Read & Write)

## 1. Ne Yaptık?
Bu görevde, ShuffleSeries platformunun içerik omurgası olan Catalog mikroservisinde hem okuma (Read) hem yazma (Write) operasyonlarını kapsayan uçtan uca CQRS komut/sorgu mimarisi, yüksek performanslı Minimal API endpoint'leri, in-memory önbellekleme ve çok dilli içerik uzlaşımı (Content Negotiation) geliştirildi:

1. **CQRS Komut ve Sorgu Mimarisi (MediatR & FluentValidation):**
   - **Movies (Filmler):** `CreateMovieCommand`, `UpdateMovieDetailsCommand`, `DeleteMovieCommand`, `GetMovieByIdQuery`, `GetMoviesListQuery`.
   - **Series (Diziler):** `CreateSeriesCommand`, `UpdateSeriesDetailsCommand`, `DeleteSeriesCommand`, `GetSeriesByIdQuery`, `GetSeriesListQuery`.
   - **Episodes (Bölümler):** `CreateEpisodeCommand`, `UpdateEpisodeDetailsCommand`, `DeleteEpisodeCommand`, `GetEpisodeByIdQuery`.
   - **Platforms (Platformlar):** `CreatePlatformCommand`, `UpdatePlatformCommand`, `DeletePlatformCommand`, `GetActivePlatformsQuery`.
   - **Moods (Ruh Halleri):** `CreateMoodCommand`, `UpdateMoodCommand`, `DeleteMoodCommand`, `GetActiveMoodsQuery`.
   - **Genres (Türler):** `CreateGenreCommand`, `UpdateGenreCommand`, `DeleteGenreCommand`, `GetActiveGenresQuery`.
   - **Media Associations & Translations:** Çeviri ekleme/güncelleme/silme (`AddOrUpdateMediaTranslationCommand`, `RemoveMediaTranslationCommand`), platform ve ruh hali ilişkilendirme (`AssociatePlatformWithMediaCommand`, `DisassociatePlatformFromMediaCommand`, `AssociateMoodWithMediaCommand`, `DisassociateMoodFromMediaCommand`).
   - **Admin CMS Listeleme:** Admin paneli için sayfalama, filtreleme, arama ve sıralama destekli `GetAdminMediaPagedListQuery` ve `AdminMediaSummaryResponse`.

2. **Minimal API Endpoint'leri (`/api/catalog/...`):**
   - `MovieEndpoints.cs`, `SeriesEndpoints.cs`, `EpisodeEndpoints.cs`, `PlatformEndpoints.cs`, `MoodEndpoints.cs`, `GenreEndpoints.cs`, `MediaAssociationEndpoints.cs`, `AdminMediaEndpoints.cs`.
   - Tüm endpoint'ler `MapGroup` ile gruplandı, OpenAPI / Swagger etiketleri, özet ve RFC 7807 problem details yanıt tipleriyle donatıldı.

3. **Read-Through In-Memory Caching (`ICatalogCacheService` / `MemoryCatalogCacheService`):**
   - Değişme sıklığı çok düşük fakat okunma sıklığı çok yüksek olan Platform, Mood ve Genre referans verileri için 2 saatlik kayan TTL ile in-memory caching uygulandı.
   - Yeni kayıt eklendiğinde, güncellendiğinde veya silindiğinde ilgili önbellek anahtarları otomatik geçersiz kılındı (cache invalidation).

4. **Çok Dilli İçerik Uzlaşımı (Content Negotiation - `ILanguageContext`):**
   - İstemciden gelen dil talebini öncelik sırasına göre çözümleyen `LanguageContext` geliştirildi:
     1. URL query parametresi (`?lang=en`)
     2. `Accept-Language` HTTP başlığı (`Accept-Language: tr-TR,tr;q=0.9`)
     3. Sistem varsayılan dili (`tr`)
     4. İngilizce fallback (`en`)
     5. İçerikte mevcut olan ilk çeviri

5. **Kültür Bağımsız Veritabanı Araması (PostgreSQL `EF.Functions.ILike`):**
   - Türkçe yerel ayarlarındaki (tr-TR) 'i' / 'İ' / 'ı' / 'I' harf dönüşümü uyumsuzluğunu gidermek için C# tarafında `.ToLower().Contains(...)` yerine veritabanı motoru seviyesinde kültür-bağımsız `EF.Functions.ILike(...)` parametrik arama operatörü uygulandı.

6. **Domain Service Refactor & Clean Architecture:**
   - Identity servisiyle mimari tutarlılık sağlanması adına domain katmanındaki `SeriesDomainService` kaldırıldı; varlık kontrolleri ve iş kuralları ilgili CQRS Handler sınıflarında koşturuldu.

7. **Security by Design:**
   - Write/mutasyon operasyonlarının tamamı JWT kimlik doğrulaması ve ince taneli yetki politikaları (`SystemPolicies.CanCreateCatalog`, `CanUpdateCatalog`, `CanDeleteCatalog`, `RequireAdminRole`) ile korunurken, read operasyonları anonim ve yüksek performanslı sunuldu.

8. **Test Kapsamı:**
   - 134 Unit Tests (`ShuffleSeries.Catalog.Tests`)
   - 33 Architecture Tests (`ShuffleSeries.ArchitectureTests`)
   - 46 Real PostgreSQL Integration Tests (`ShuffleSeries.Catalog.IntegrationTests` - WebApplicationFactory + Testcontainers + Respawn)
   - Toplam 557 çözüm testinin tamamı başarıyla yeşile ulaştı.

---

## 2. Neden Yaptık?
1. **Mikroservis Performansı ve GC Optimizasyonu:**
   - Ağır Controller yapıları ve reflection maliyetleri yerine ASP.NET Core Minimal API kullanılarak bellek ayak izi minimize edildi.
   - Sıkça sorgulanan Platform, Mood ve Genre verileri in-memory cache'e alınarak veritabanı I/O yükü ve bağlantı havuzu tüketimi düşürüldü.
2. **Kusursuz Kullanıcı Deneyimi ve Çok Dillilik:**
   - Kullanıcıların cihaz dillerine veya arayüz seçimlerine göre anında doğru dildeki içerik metadata'sını alabilmesi sağlandı; çevirisi bulunmayan dillerde hata vermek yerine zarif bir şekilde varsayılan dile (fallback) düşüldü.
3. **Güvenlik ve OWASP Standartları:**
   - Yetkisiz kişilerin veya standart kullanıcıların katalog içeriğini değiştirmesini engellemek için JWT claim bazlı RBAC (Role-Based Access Control) ve yetki politikaları zorunlu kılındı.
   - SQL enjeksiyon risklerini tamamen bertaraf etmek için EF Core parametrik sorgulaması ve ILIKE operatörü kullanıldı.
4. **Tutarlılık ve Idempotency:**
   - Veritabanı seviyesinde soft delete uygulandı; silinen varlıklar Global Query Filter ile sorgulardan gizlendi. Başlık ve IMDb ID tekillikleri her mutasyon öncesinde denetlendi.

---

## 3. Hangi Pattern'leri Kullandık?
| Pattern | Kullanım Amacı |
| :--- | :--- |
| **CQRS (Command Query Responsibility Segregation)** | Okuma ve yazma işlemlerinin model, DTO ve handler seviyesinde tamamen ayrılması. |
| **Mediator Pattern (MediatR)** | Minimal API endpoint'leri ile iş mantığı katmanı arasındaki doğrudan bağımlılığın gevşetilmesi. |
| **Read-Through Caching & Cache Invalidation** | Referans katalog verilerinin önbellekten sunulması ve değişikliklerde anında temizlenmesi. |
| **Ambient Context (Content Negotiation)** | HTTP Request scope'unda dil bağlamının (`ILanguageContext`) çözümlenip handler'lara enjekte edilmesi. |
| **Pipeline Behavior (Validation Behavior)** | Gelen komutların handler'a ulaşmadan önce FluentValidation kurallarıyla otomatik doğrulanması. |
| **Global Query Filter & Soft Delete** | Silinen kayıtların fiziksel olarak yok edilmeden mantıksal olarak sorgulardan izole edilmesi. |
| **Policy-Based Authorization** | İnce taneli sistem politikalarıyla katalog yazma operasyonlarının yetkilendirilmesi. |

---

## 4. Yapılan Geliştirme Nasıl Test Edilir?

### A. Birim Testleri (Unit Tests)
- **Komut:** `dotnet test ShuffleSeries.Catalog/ShuffleSeries.Catalog.Tests`
- **Kapsam:**
  - `CreateMovieCommandHandlerTests`, `UpdateMovieDetailsCommandHandlerTests`, `DeleteMovieCommandHandlerTests`: Film komutlarının iş kuralı doğrulamaları, varlık kontrolleri ve repository mock etkileşimleri.
  - `CreateSeriesCommandHandlerTests`, `GetSeriesByIdQueryHandlerTests`: Dizi aggregate komut/sorgu testleri.
  - `CreateEpisodeCommandHandlerTests`: Bölüm ekleme ve dizi aggregate üzerinde bölüm oluşturma mock testleri.
  - `CreatePlatformCommandHandlerTests`, `GetActivePlatformsQueryHandlerTests`: Platform CQRS ve cache etkileşim testleri.
  - `LanguageContextTests`: Query param, HTTP header ve varsayılan dil çözümleme öncelik testleri.

### B. Mimari Testleri (Architecture Tests)
- **Komut:** `dotnet test ShuffleSeries.ArchitectureTests`
- **Kapsam:**
  - Onion Architecture katman bağımlılıklarının ihlal edilmemesi (Domain bağımsızlığı).
  - MediatR Request Handler'ların `sealed` olması ve doğru katmanda yer alması.
  - Entity'lerin doğrudan API dışına sızdırılmaması (DTO kullanımı).

### C. Gerçek Entegrasyon Testleri (Testcontainers + Respawn)
- **Komut:** `dotnet test ShuffleSeries.Catalog/ShuffleSeries.Catalog.IntegrationTests`
- **Kapsam:**
  - `MovieEndpointsIntegrationTests`: Film oluşturma (201 Created), içerik uzlaşımıyla getirme (200 OK), güncelleme (204 No Content), silme (204 No Content), geçersiz veri validasyonu (400 Bad Request) ve yetkisiz erişim kontrolleri (401/403).
  - `SeriesEndpointsIntegrationTests`: Dizi oluşturma, detay getirme, güncelleme ve silme doğrulamaları.
  - `EpisodeEndpointsIntegrationTests`: Diziye bölüm ekleme, sezon otomatik oluşturma, çok dilli bölüm detayı getirme, güncelleme ve soft-delete doğrulamaları.
  - `PlatformMoodGenreEndpointsIntegrationTests`: Platform, Mood ve Genre CRUD operasyonları, Read-Through Memory Cache doğrulaması ve cache invalidation testleri.
  - `MediaAssociationEndpointsIntegrationTests`: Medyaya dinamik çeviri ekleme/silme, platform bağlama/çıkarma ve ruh hali ilişkilendirme doğrulamaları.
  - `AdminMediaEndpointsIntegrationTests`: Admin sayfalama, filtreleme, kültür-bağımsız başlık araması ve yetkilendirme doğrulamaları.
