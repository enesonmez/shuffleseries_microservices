# Task 3.1: Çok Dilli Domain Modellemesi, Veritabanı ve Seed Altyapısı

## 1. Ne Yaptık?
Bu görevde, ShuffleSeries platformunun içerik merkezi ve "Single Source of Truth" (SSOT) noktası olan Catalog mikroservisinde zengin Domain-Driven Design (DDD) ve Onion Architecture standartlarına uygun olarak:
1. **Çok Dilli (Multi-Language) Mimari (Entity Translation Pattern):**
   - Sabit kolonlu dil desteği (TitleTr, TitleEn) yerine, gelecekte yeni dillerin (Almanca, İspanyolca vb.) veritabanı şemasında DDL/Migration değişikliği gerektirmeden eklenebilmesi için `MediaTranslation`, `MoodTranslation` ve `EpisodeTranslation` entity'leri geliştirildi.
   - Diller ISO 639-1 formatında küçük harfle (`tr`, `en`) normalize edilerek bileşik benzersiz indekslerle (`(MediaId, LanguageCode)`) koruma altına alındı.
2. **TPH (Table-per-Hierarchy) Tabanlı Zengin Medya Modellemesi:**
   - Ortak metadata'ları (IMDb puanı, oy sayısı, çıkış yılı, afiş/arka plan görselleri, çeviriler, platform ve ruh hali ilişkileri) barındıran soyut `Media` Aggregate Root'u tasarlandı.
   - `Media` sınıfından türeyen `Movie` (`RuntimeMinutes`, `Director`) ve `Series` (`SeriesType`, `TotalSeasons`, `Seasons`) modelleri oluşturuldu.
3. **Bölüm Ruleti & Antoloji Serileri İçin Hiyerarşik Bölüm Modellemesi:**
   - Dizi altında `Season` ve sezon altında `Episode` entity hiyerarşisi kuruldu.
   - PRD Bölüm 1 ve 5.3 gereksinimlerine uygun olarak `SeriesType` (`Standard`, `EpisodicAnthology`, `SeasonalAnthology`) enum'ı ve bölüm seviyesinde `IsStandalone` flag'i eklendi.
   - Episodic Anthology dizilerinde (örn: *Black Mirror*, *Love Death & Robots*) bölümler eklendiğinde varsayılan olarak `IsStandalone = true` atanması iş kuralı domain seviyesinde kapsüllendi.
   - Shuffle Engine'in milisaniyenin altında bağımsız bölümlere erişebilmesi için PostgreSQL seviyesinde filtrelenmiş kısmi indeks (`CREATE INDEX ... WHERE "IsStandalone" = true AND "IsDeleted" = false`) tanımlandı.
4. **Dijital Yayın Platformları (Streaming Platforms):**
   - `Platform` Aggregate Root'u oluşturuldu. Aktif/pasif SVG logoları, web yönlendirme URL'leri ve mobil deep link şemaları (`netflix://`, `primevideo://` vb.) modellendi.
   - Çoka-çok ilişkiler için `MediaPlatform` ve bölüme özel yönlendirmeler için `EpisodePlatform` junction tabloları oluşturuldu.
5. **Ruh Halleri (Moods) & Dinamik Atmosfer Işıkları:**
   - PRD Onboarding gereksiniminde belirtilen his odaklı 9 ruh hali (`kafa-dagitmalik`, `icini-isitanlar`, `beyin-yakanlar`, `zeka-oyunlari`, `gaza-getirenler`, `gerim-gerim-gerenler`, `dunyadan-uzaklas`, `gozyasi-garantili`, `ufkunu-genislet`) hex renk kodları (`AmbientColorHex`), ikonları ve çok dilli isim/özet/etiketleriyle modellendi.
6. **Platform-Bağımsız Tür Taksonomisi (Dual-Taxonomy - Genres):**
   - Objektif içerik türlerini (Aksiyon, Komedi, Bilim Kurgu vb.) temsil eden ve 3. parti sağlayıcı kimliklerinden arındırılmış `Genre` Aggregate Root'u ve `GenreTranslation` entity'leri geliştirildi.
   - Medya ile tür arasındaki çoka-çok ilişki `MediaGenre` junction tablosu ve ters FK B-Tree indeksleriyle kuruldu (`Media.AddGenre`, `Media.RemoveGenre`).
   - Sektör standardı olan 19 TMDB türü hem Türkçe hem İngilizce olarak platform-bağımsız referans kodlarıyla tohumlandı; örnek yapımlara (`Inception`, `Black Mirror`) tür ilişkileri atandı.
7. **Veritabanı Migration & Idempotent Seeder:**
   - `AddCatalogFullDomainModelingAndMultiLanguage` ve `AddCatalogGenreSupport` EF Core migration'ları oluşturuldu.
   - Uygulama ayağa kalktığında (`DatabaseMigrationExtensions`) ve entegrasyon testlerinde (`Testcontainers` + `Respawn`) 9 ruh halini, 5 streaming platformunu, 19 evrensel türü ve örnek içerikleri (*Inception*, *Black Mirror*) otomatik yükleyen idempotent `CatalogDbSeeder` entegre edildi.
8. **Test Kapsamı:**
   - 93 Unit Test (`Catalog.Tests`), 31 Architecture Test (`ArchitectureTests`) ve 22 Real PostgreSQL Integration Test (`Catalog.IntegrationTests`) başarıyla tamamlandı (Toplam: 146 Catalog testi, genel çözüm toplamı: 489 test).

---

## 2. Neden Yaptık?
1. **Genişletilebilir Çok Dillilik:**
   - Klasik yaklaşımda tablolara `Title_TR`, `Title_EN`, `Title_DE` gibi kolonlar eklenir. Bu yaklaşım her yeni dilde veritabanı migration'ı, kod refactor'ı ve boş (NULL) kolon maliyeti doğurur. **Entity Translation Pattern** sayesinde sistem sıfır şema değişikliğiyle 50+ dili destekleyebilir.
2. **Performans ve Kartezyen Patlama (Cartesian Explosion) Önleme:**
   - Bir medya çekilirken çevirileri, platformları, ruh halleri ve sezon/bölümleri aynı sorguda `JOIN` yapılırsa satır sayısı katlanarak (örneğin 2 çeviri * 3 platform * 2 mood = 12 kat) PostgreSQL ve bellek tüketimini patlatır. Bu nedenle repository katmanında `AsSplitQuery()` zorunlu kılındı.
3. **Milisaniyelik Bölüm Ruleti (Filtered Partial Index):**
   - Milyonlarca bölüm arasından sadece bağımsız olanları (`IsStandalone = true`) ve silinmemişleri getirmek için tüm tabloyu taramak yerine, sadece ilgili kayıtları tutan küçük bir B-Tree partial index oluşturuldu. Bu, indeks boyutunu %90 oranında küçültürken sorgu süresini mikrosaniye seviyesine indirir.
4. **Domain Encapsulation (Zengin Domain Modeli):**
   - Anemic domain modelden kaçınılarak tüm setter'lar kapatıldı. Medyaya platform ekleme (`AddPlatform`), çeviri güncelleme (`AddOrUpdateTranslation`) ve bölüm oluşturma (`AddEpisode`) aggregate root sınırları içerisindeki iş kurallarıyla güvenceye alındı.

---

## 3. Hangi Pattern'leri Kullandık?
| Pattern | Kullanım Amacı |
| :--- | :--- |
| **Entity Translation Pattern** | Çok dilli içeriklerin (TR/EN) şema değiştirmeden dinamik yönetilmesi. |
| **Table-per-Hierarchy (TPH)** | `Media`, `Movie` ve `Series` hiyerarşisinin tek tabloda yüksek okuma performansıyla saklanması. |
| **Aggregate Root & Child Entity (DDD)** | `Series` Aggregate Root'unun `Season` ve `Episode` yaşam döngüsünü tam yetkiyle yönetmesi. |
| **Domain Events & Outbox Pattern** | Varlık oluşturma, güncelleme ve silme olaylarının (`MediaCreatedDomainEvent`) MassTransit/RabbitMQ'ya güvenle fırlatılması. |
| **Partial Filtered Index (PostgreSQL)** | Sadece `IsStandalone = true` ve `IsDeleted = false` kayıtlar için optimize edilmiş indeks. |
| **Split Query Optimization (`AsSplitQuery`)** | Çoklu koleksiyon yüklemelerinde kartezyen çarpımı engelleme. |
| **Idempotent Database Seeder** | Başlangıç mood ve platform verilerinin varlık kontrolleri yapılarak güvenle eklenmesi. |

---

## 4. Yapılan Geliştirme Nasıl Test Edilir?

### A. Birim Testleri (Unit Tests)
- **Komut:** `dotnet test ShuffleSeries.Catalog/ShuffleSeries.Catalog.Tests`
- **Kapsam:**
  - `SeriesTests`, `MovieTests`, `EpisodeTests`, `PlatformTests`, `MoodTests`, `GenreTests`: Rich domain iş kuralları, validasyonlar, domain event fırlatma kontrolleri.
  - `CreateSeriesCommandValidatorTests`, `UpdateSeriesCommandValidatorTests`: FluentValidation kurallarının sınır değer testleri.
  - `CreateSeriesCommandHandlerTests`, `UpdateSeriesCommandHandlerTests`, `DeleteSeriesCommandHandlerTests`, `GetSeriesByIdQueryHandlerTests`, `GetSeriesListQueryHandlerTests`: CQRS handler mock testleri.
  - `CatalogExceptionsTests`: RFC 7807 problem details hata kodlarının doğrulanması.

### B. Mimari Testleri (Architecture Tests)
- **Komut:** `dotnet test ShuffleSeries.ArchitectureTests`
- **Kapsam:**
  - Domain katmanının üst katmanlardan tamamen izole olması (Onion Architecture).
  - Tüm entity'lerin `BaseEntity` veya `AggregateRoot`'tan türemesi.
  - Entity property'lerinin public setter içermemesi (Encapsulation).
  - Tüm Repository arayüzlerinin `IRepository` marker'ını uygulaması.
  - MediatR handler sınıflarının `sealed` ve `Handler` sonekiyle bitmesi.

### C. Gerçek Entegrasyon Testleri (Testcontainers + Respawn)
- **Komut:** `dotnet test ShuffleSeries.Catalog/ShuffleSeries.Catalog.IntegrationTests`
- **Kapsam:**
  - `Database_ShouldContainSeededMoodsAndPlatforms_OnStartup`: PostgreSQL container'ı ayağa kalktığında 9 ruh hali ve 5 platformun veritabanına başarıyla yazıldığı doğrulanır.
  - `SeedData_ShouldContainAll19StandardGenres_WithTurkishAndEnglishTranslations`: 19 TMDB evrensel türünün TR ve EN çevirileriyle eksiksiz tohumlandığı doğrulanır.
  - `SeedData_SampleMedia_ShouldHaveGenresAssociated`: Örnek filmlerin (Inception) ve dizilerin (Black Mirror) türlerle doğru bağlandığı test edilir.
  - `GenreRepository_ShouldResolveAndQueryActiveGenres`: `IGenreRepository`'nin DI üzerinden çözülerek aktif türleri çevirileriyle getirdiği test edilir.
  - `CreateSeries_WithValidPayload_ShouldPersistInRealPostgreSql_AndReturn201Created`: Dizi ve çeviri kayıtlarının gerçek veritabanına yazıldığı ve `MediaCreatedDomainEvent` outbox mesajının jsonb formatında oluştuğu doğrulanır.
  - `GetSeriesById_WhenSeriesExists_ShouldReturn200Ok_AndCorrectData`: İlişkili çeviri ve metadata'ların API yanıtında eksiksiz döndüğü test edilir.
  - `DeleteSeries_ShouldSoftDeleteInPostgreSql_AndHideFromGetEndpoint`: Soft delete sonrası kaydın Global Query Filter ile gizlendiği, veritabanında `IsDeleted=true` kaldığı doğrulanır.
