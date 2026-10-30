# Task 3.2.2: Catalog TMDb & IMDb Zorunluluğu ve Başlık (Title) Tekilliğinin Kaldırılması

## 1. Ne Yaptık?
Katalog servisinin film (`Movie`) ve dizi (`Series`) varlık modelinde ve iş kurallarında endüstri standartlarına (TMDb / IMDb) ve gerçek dünya gereksinimlerine uygun kapsamlı bir mimari iyileştirme gerçekleştirdik:
1. **Title Tekilliği Kaldırıldı:** Sinema ve televizyon dünyasında aynı ada sahip bağımsız yapımlar (*Crash* 1996 vs 2004, *Heat* 1986 vs 1995, *The Avengers* 1998 vs 2012) ve yeniden çekimler (remake) sıkça yer aldığından; `MovieTitleAlreadyExistsException`, `SeriesTitleAlreadyExistsException` ve `ExistsByTitleAsync` kontrolleri domain, application, repository ve test katmanlarından tamamen kaldırıldı.
2. **`TmdbId` Zorunlu Kılındı:** The Movie Database entegrasyonu için `Media` kök varlığına `TmdbId` (integer, `> 0`) alanı eklendi, FluentValidation kuralı tanımlandı ve veritabanı seviyesinde `IsDeleted = false` filtrelenmiş tekil indeks (`Partial Unique Index`) oluşturuldu.
3. **`ImdbId` Zorunlu Kılındı (NOT NULL):** Önceden `string?` (nullable) olan IMDb kimliği, zorunlu (`string`, format: `^tt\d{7,10}$`) hale getirildi ve yine `IsDeleted = false` filtrelenmiş tekil indeks uygulandı.
4. **EF Core Migration & Seeding:** `20261024102151_AddTmdbIdAndMakeImdbIdRequired` migration'ı üretildi ve tohum verileri (`Inception`: TMDb 27205, IMDb `tt1375666`; `Black Mirror`: TMDb 42009, IMDb `tt2085059`) güncellendi.
5. **Tüm Testler & E2E Suite Güncellendi:** Domain birim testleri (298 test), Mimari kurallar (37 test), WebApplicationFactory + Testcontainers entegrasyon testleri (56 test) ve Python Gateway E2E suite'i %100 başarıyla güncellendi.

---

## 2. Neden Yaptık? (Mimari Kararlar ve Rasyonel)
- **Domain Gerçekliği:** Bir film kataloğunda "Başlık" (Title) iş kimliği (Business Key / Natural Key) olamaz. Farklı yönetmenlerce, farklı yıllarda çekilen filmler aynı ismi taşıyabilir. Çeviri başlıkları da diller arası çakışabilir.
- **TMDb ve IMDb Standardizasyonu:** Harici metadata ve poster/backdrop senkronizasyonunun ana kaynağı The Movie Database (TMDb) API'sidir (`/movie/{id}` ve `/tv/{id}/external_ids`). Buradan gelen veride `tmdb_id` ve `imdb_id` mutlak tekil kimliklerdir.
- **Soft-Delete Uyumluluğu:** Mantıksal olarak silinmiş (`IsDeleted = true`) bir yapımın `TmdbId` veya `ImdbId`'si veritabanında kalmaya devam eder. Eğer unconditional `UNIQUE` indeks konulsaydı, silinen bir yapım yeniden eklenemezdi. Bu nedenle PostgreSQL partial index (`HasFilter("\"IsDeleted\" = false")`) uygulandı.

---

## 3. Hangi Pattern'leri Kullandık?
- **DDD Aggregate Root & Encapsulation:** `Media` abstract sınıfı üzerinde `TmdbId` ve `ImdbId` alanları kapsüllendi; mutasyonlar `UpdateRatings(tmdbId, imdbId, ...)` ve `Movie.Create(...)` gibi iş odaklı fabrika/domain metotlarıyla sınırlandırıldı.
- **Partial / Filtered Index Pattern:** PostgreSQL'in koşullu indeksleme yeteneği kullanılarak yalnızca aktif (`IsDeleted = false`) kayıtlarda tekillik garanti edildi.
- **RFC 7807 ProblemDetails & ConflictException:** Mükerrer TMDb veya IMDb eklemelerinde `DuplicateTmdbIdException` ve `DuplicateImdbIdException` fırlatılarak standart `409 Conflict` durum kodu dönüldü.
- **FluentValidation Regex Guard:** IMDb kimlikleri için `^tt\d{7,10}$` ve TMDb için `> 0` sınır kontrolleri yapılarak sisteme kirli veri girişi uygulama sınırında engellendi.

---

## 4. Yapılan Geliştirme Nasıl Test Edilir?

### A. Terminal Komutları (Doğrulama Kanıtları)
```bash
# 1. Catalog Birim Testleri (298/298 PASS)
dotnet test ShuffleSeries.Catalog/ShuffleSeries.Catalog.Tests/ShuffleSeries.Catalog.Tests.csproj

# 2. Mimari Sınır Testleri (37/37 PASS)
dotnet test ShuffleSeries.ArchitectureTests/ShuffleSeries.ArchitectureTests.csproj

# 3. Gerçek Testcontainers PostgreSQL Entegrasyon Testleri (56/56 PASS)
dotnet test ShuffleSeries.Catalog/ShuffleSeries.Catalog.IntegrationTests/ShuffleSeries.Catalog.IntegrationTests.csproj

# 4. Çözüm Genel Test Koşusu (491/491 PASS)
dotnet test ShuffleSeries.sln
```

### B. Örnek Test Case'leri (HTTP / cURL)
1. **Aynı İsimle İki Farklı Film Ekleyebilme (Happy Path):**
```http
POST /api/catalog/movies
{
  "tmdbId": 120,
  "imdbId": "tt0120737",
  "releaseYear": 2001,
  "runtimeMinutes": 178,
  "posterUrl": "https://example.com/lotr1.jpg",
  "languageCode": "tr",
  "title": "Yüzüklerin Efendisi",
  "overview": "Yüzük Kardeşliği"
}
# Status: 201 Created

POST /api/catalog/movies
{
  "tmdbId": 121,
  "imdbId": "tt0167261",
  "releaseYear": 2002,
  "runtimeMinutes": 179,
  "posterUrl": "https://example.com/lotr2.jpg",
  "languageCode": "tr",
  "title": "Yüzüklerin Efendisi",
  "overview": "İki Kule (Aynı ana başlıkla farklı film eklenebilmelidir)"
}
# Status: 201 Created (Önceden 409 Conflict veriyordu, artık başarıyla eklenir!)
```

2. **Mükerrer TMDb ID Engelleme (409 Conflict):**
```http
POST /api/catalog/movies
{
  "tmdbId": 120,
  "imdbId": "tt9999999",
  ...
}
# Status: 409 Conflict -> code: "DUPLICATE_TMDB_ID"
```

3. **Geçersiz IMDb ID Formatı (400 Bad Request):**
```http
POST /api/catalog/movies
{
  "tmdbId": 500,
  "imdbId": "invalid_imdb",
  ...
}
# Status: 400 Bad Request -> "IMDb ID 'tt' ile başlamalı ve ardından 7-10 rakam içermelidir."
```
