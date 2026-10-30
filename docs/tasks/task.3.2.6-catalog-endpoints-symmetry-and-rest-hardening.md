# Task 3.2.6: Catalog Servisi Endpoint Simetrisi, REST Sertleştirmesi ve DTO İzolasyonu

## Ne Yaptık?
Catalog mikroservisinde tespit edilen ve mimari tutarlılığı zedeleyen 4 temel asimetri uçtan uca giderilerek sistem tam simetrik, güvenli ve REST standartlarına tam uyumlu hale getirilmiştir:

1. **İş Mantığı ve Kaynak Oluşturma Simetrisi (Kritik):**
   - `CreateMovieCommand`'da bulunan opsiyonel taksonomi bağlama yeteneği (`PlatformIds`, `MoodIds`, `GenreIds`) `CreateSeriesCommand`'a da kazandırılmıştır.
   - Dizi oluşturulurken tek seferde toplu veritabanı doğrulaması (`GetByIdsAsync`) yapılarak $O(1)$ bellek eşleşmesiyle platform, ruh hali ve tür ilişkilendirmeleri yapılmıştır.
2. **Çoklu Dil ve Çeviri Yönetimi Simetrisi (Kritik):**
   - `Media` ve `Episode` varlıklarında var olan granüler çeviri yönetim yeteneği `Mood` ve `Genre` taksonomilerine de genişletilmiştir.
   - `POST /api/catalog/moods/{id}/translations` ve `DELETE /api/catalog/moods/{id}/translations/{lang}`
   - `POST /api/catalog/genres/{id}/translations` ve `DELETE /api/catalog/genres/{id}/translations/{lang}`
   - Domain entity seviyesinde `AddOrUpdateTranslation` ve `RemoveTranslation` metotları eklenerek kapsülleme korunmuştur.
3. **REST Standartları ve HTTP Header Simetrisi (Orta Seviye):**
   - `Platform`, `Mood` ve `Genre` taksonomileri için tekil `GET /{id:guid}` endpoint'leri eklenmiştir (`GetPlatformById`, `GetMoodById`, `GetGenreById`).
   - Repository sözleşmelerine EF Core sızıntısı (`asNoTracking` parametresi) yapılmadan, `GetByIdForDetailAsync(Guid id, CancellationToken ct)` metoduyla altyapı katmanında `.AsNoTracking()` çağrısı kapsüllenmiştir.
   - `POST` oluşturma endpoint'leri (`PlatformEndpoints`, `MoodEndpoints`, `GenreEndpoints`) ham URI string'i yerine standart `Results.CreatedAtRoute("Get*ById", new { id }, id)` ile `Location` başlığı üretecek şekilde güncellenmiştir.
4. **DTO ve İsimlendirme Standartları Simetrisi (Temizlik & Sertleştirme):**
   - Karışıklığa sebep olan `SeriesResponse` kaydı `SeriesDetailResponse` olarak yeniden adlandırılmış; alt tipleri DTO kurallarına uygun olarak `SeriesSeasonDto`, `SeriesEpisodeDto`, `SeriesPlatformDto` vb. dönüştürülmüştür.
   - Film ve dizi güncelleme endpoint'lerine (`UpdateMovieRequest`, `UpdateSeriesRequest`) opsiyonel `PosterUrl` ve `BackdropUrl` güncelleyebilme desteği eklenmiştir.
   - FluentValidation ile otomatik 400 ProblemDetails üretilmesine rağmen OpenAPI'de mükerrer tanımlanan `.ProducesProblem(StatusCodes.Status400BadRequest)` çağrıları temizlenmiştir.

---

## Neden Yaptık?
- **Geliştirici Deneyimi ve İstemci Öngörülebilirliği:** Frontend ve mobil istemcilerin bir kaynakta (film) kullandığı deseni diğer kaynakta (dizi veya taksonomi) bulamaması entegrasyon hatalarına yol açar. Dizi oluşturulurken platform ve türlerin ayrı isteklerle bağlanmak zorunda bırakılması gereksiz gidiş-dönüş (roundtrip) ve ağ maliyeti oluşturuyordu.
- **REST Standartları ve HATEOAS Uyumluluğu:** HTTP 201 Created yanıtı, oluşturulan kaynağın tekil URI'sini `Location` başlığında döndürmelidir. Ancak kaynağın tekil bir `GET /{id}` uç noktası yoksa bu `Location` başlığı anlamsızlaşır ve geçersiz hale gelir. Tekil detay sorgularının açılmasıyla REST simetrisi sağlanmıştır.
- **Mimari Sızıntıların Önlenmesi (Clean Architecture / Ponytail):** EF Core'un `AsNoTracking` davranışını `bool asNoTracking = false` parametresiyle Domain katmanındaki `IRepository` sözleşmelerine sızdırmak Onion Architecture prensiplerini ihlal eder. Bunun yerine salt okuma amacına yönelik `GetByIdForDetailAsync` metodu tanımlanarak ORM detayları Infrastructure katmanında gizlenmiştir.

---

## Hangi Pattern'leri Kullandık?
- **CQRS & Mediator Pattern:** Yeni eklenen tekil okumalar (`GetPlatformByIdQuery`, `GetMoodByIdQuery`, `GetGenreByIdQuery`) ve çeviri komutları (`AddOrUpdateMoodTranslationCommand`, `RemoveMoodTranslationCommand` vb.) bağımsız handler'larla ayrılmıştır.
- **Encapsulated Entity Mutators (DDD):** Çeviri ekleme ve çıkarma işlemleri doğrudan koleksiyon manipülasyonu yerine `Mood.AddOrUpdateTranslation`, `Mood.RemoveTranslation`, `Genre.AddOrUpdateTranslation` ve `Genre.RemoveTranslation` zengin domain metotları üzerinden gerçekleştirilmiştir.
- **Repository Abstraction:** Read işlemlerinde `AsNoTracking()` davranışı `GetByIdForDetailAsync` metodu ile altyapıda izole edilmiştir.
- **Performance Guard (Sıfır N+1):** Diziye taksonomi bağlarken döngü içi sorgu atılmamış; `GetByIdsAsync` ile toplu çekilip bellekte $O(1)$ eşleştirilmiştir.

---

## Nasıl Test Edilir?
### 1. Otomatik Testler
```bash
# Tüm xUnit Birim ve Entegrasyon Testleri (842 Test)
dotnet test ShuffleSeries.sln

# Mimari Kuralların Doğrulanması (40 Test)
dotnet test ShuffleSeries.ArchitectureTests

# API Gateway Üzerinden Uçtan Uca (E2E) Test Suite (123 Test)
python3 e2e_tests/run_all.py
```

### 2. E2E Test Senaryoları (Test Matrix)
| Kategori | Metot | Endpoint | Beklenen Durum | Açıklama |
|---|---|---|---|---|
| Platforms | POST | `/api/catalog/platforms` | 201 Created | `Location: /api/catalog/platforms/{id}` döner |
| Platforms | GET | `/api/catalog/platforms/{id}` | 200 OK | Platform tekil detayını getirir |
| Platforms | GET | `/api/catalog/platforms/{random-id}` | 404 Not Found | Olmayan ID için ProblemDetails döner |
| Moods | POST | `/api/catalog/moods/{id}/translations` | 204 NoContent | Ruh haline yeni dil çevirisi ekler |
| Moods | DELETE | `/api/catalog/moods/{id}/translations/{lang}` | 204 NoContent | Ruh halinden dil çevirisini kaldırır |
| Moods | GET | `/api/catalog/moods/{id}` | 200 OK | Dil çevirileriyle birlikte tekil detay |
| Genres | POST | `/api/catalog/genres/{id}/translations` | 204 NoContent | Türe yeni dil çevirisi ekler |
| Genres | DELETE | `/api/catalog/genres/{id}/translations/{lang}` | 204 NoContent | Türden dil çevirisini kaldırır |
| Genres | GET | `/api/catalog/genres/{id}` | 200 OK | Tür tekil detayını getirir |
| Series | POST | `/api/catalog/series` | 201 Created | Platform, mood ve genre ID'leri ile tek seferde dizi oluşturur |
| Series | PUT | `/api/catalog/series/{id}` | 204 NoContent | PosterUrl ve BackdropUrl dahil dizi detaylarını günceller |
