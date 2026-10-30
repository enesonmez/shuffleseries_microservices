# Task 3.2.1: Catalog Servisi Granüler İzinleri (PBAC), Rol-İzin Eşlemeleri ve Identity DB Seed Mimarisi

Bu doküman; ShuffleSeries mikroservis ekosisteminde Catalog Bounded Context'i için hazırlanan tüm Minimal API endpoint'lerinin granüler izinler (Fine-Grained Permission-Based Access Control) ile güvence altına alınmasını, sistem rollerine (`Guest`, `Standard`, `Premium`, `Admin`) göre izinlerin dağıtılmasını ve `IdentityDbSeeder` üzerinden veritabanına otomatik tohumlanmasını detaylandırır.

---

## 1. Ne Yaptık?
* **Single Source of Truth (SSOT) İzin Sabitleri:** `ShuffleSeries.Shared.Core.Domain.Constants.SystemPermissions.Catalog` altında her kaynak (Movies, Series, Episodes, Platforms, Moods, Genres, Associations, AdminMedia) ve CRUD eylemi için tip güvenli granüler izinler tanımlandı.
* **Yalın Mimari ve Ölü Kod Temizliği (YAGNI & Dead Code Elimination):** Hiçbir endpoint tarafından tüketilmediği tespit edilen genel şemsiye izinler (`catalog:read`, `catalog:create`, `catalog:update`, `catalog:delete`) ve eski genel politikalar (`CanReadCatalog`, `CanCreateCatalog`, `CanUpdateCatalog`, `CanDeleteCatalog`) sistemden tamamen arındırıldı. Her endpoint doğrudan kendi atomik granüler iznine bağlandı.
* **Tüm Endpoint'lerin Güvenceye Alınması:** İstemci okuma (GET) endpoint'leri dahil (`/movies/{id}`, `/series/{id}`, `/platforms`, `/moods`, `/genres`) tüm uç noktalar yetkilendirme altına alındı. OpenAPI metadata'larına RFC 7807 `401 Unauthorized` ve `403 Forbidden` yanıt tipleri eklendi.
* **Admin CMS Ayrımı:** Yönetimsel `/api/catalog/admin/media` endpoint'i `CanReadAdminMedia` politikası ile korunarak hem `Admin` rolüne hem de `catalog:admin:read` iznine sahip backoffice kullanıcılarına açıldı.
* **Identity Veritabanı Tohumlama (Seeding & Idempotency):** `IdentityDbSeeder.cs` güncellenerek 24 yeni izin eklendi. `Guest`, `Standard`, `Premium` ve `Admin` rollerinin yetki kümeleri güncellendi; seeder'ın tekrar çalıştırmalarda mükerrer kayıt üretmeyen idempotent yapısı korundu.
* **Sır Yönetimi & Vault SSOT (API8 Zero Hardcoded Secrets):** Catalog servisinin `appsettings.json` dosyasındaki hardcoded `Jwt` bloğu tamamen kaldırıldı. Tıpkı Identity servisinde olduğu gibi JWT anahtarları ve yapılandırmaları (`Jwt:Secret`, `Jwt:Issuer`, `Jwt:Audience`) HashiCorp Vault (`shuffleseries/shared`) üzerinden çalışma anında yüklenecek şekilde yapılandırıldı.
* **Kapsamlı Test Doğrulaması:** `ShuffleSeries.Identity.Tests` projesine `IdentityDbSeederTests` eklendi. Catalog entegrasyon testlerine 401 Unauthorized, 403 Forbidden ve granüler okuma izin testleri eklendi. `CatalogApiFactory` entegrasyon ortamında test ortamı JWT değişkenlerini statik yapılandırıcıyla izole etti. Toplam 563 test hatasız geçti.

---

## 2. Neden Yaptık? (Mimari Kararlar ve Teknik Nedenler)

### Neden Yalnızca Rol Tabanlı (RBAC) Değil, İzin Tabanlı (PBAC / Hybrid RBAC+PBAC) Yetkilendirme?
Sistemde yetkilendirmeyi yalnızca statik roller (`Role == "Admin"`) üzerinden kurgulamak, Açık/Kapalı Prensibini (Open/Closed Principle) ihlal eder ve ileride sisteme dahil olacak "İçerik Editörü", "Kategori Yöneticisi", "Moderator" veya "Destek Temsilcisi" gibi ara rolleri desteklemeyi imkansız kılar. 
Catalog servisindeki endpoint'lerin salt rollere değil, atomik izinlere (`catalog:movies:create`, `catalog:platforms:update` vb.) bakması sayesinde:
1. Endpoint'ler gelecekte eklenecek yeni rollerden etkilenmez.
2. Backoffice üzerinde dinamik olarak yeni roller oluşturulabilir veya tekil kullanıcılara rol dışı özel izinler atanabilir (`UserPermission`).
3. JWT içine kullanıcının o an sahip olduğu etkin izinler (`permissions`) enjekte edildiğinden, Catalog servisi her HTTP isteğinde Identity veritabanına sorgu atmak zorunda kalmaz (Zero Network Latency & High Performance).

### Neden Okuma (GET) Endpoint'leri de `catalog:*:read` İzniyle Korundu?
ShuffleSeries iş modelinde (PRD) platformdaki içerik metaverileri ve izleme modları, kimliği doğrulanmış oturumlara (Anonim mobil `Guest` oturumu veya kayıtlı `Standard`/`Premium` oturumu) sunulur. Ziyaretçi uygulamayı ilk açtığında arka planda sürtünmesiz bir Guest JWT üretilir. İstemci okuma endpoint'lerinin de koruma altına alınması:
1. Yetkisiz bot taramalarını ve API kazımayı (scraping) engeller.
2. API Gateway üzerinde JWT tabanlı Rate Limiting ve Blacklist filtrelerinin uçta (Edge) devreye girmesini sağlar.
3. İleride misafir veya standart kullanıcılara yönelik özelleştirilmiş görünürlük/bilet kısıtlamalarının aynı boru hattından yönetilebilmesine zemin hazırlar.

### Neden Genel Şemsiye İzinler Kaldırıldı ve Saf Granüler Yapıya Geçildi?
```csharp
authBuilder.AddPolicy(SystemPolicies.CanReadCatalogMovies, policy =>
    policy.RequireClaim("permission", SystemPermissions.Catalog.MoviesRead));
```
Katalog servisindeki tüm Minimal API endpoint'leri granüler politikalara bağlandığında, eski kaba genel izinler (`catalog:read`, `catalog:create` vb.) atıl hale geldi (dead code). Bu şemsiye izinlerin sistemde tutulması yetki denetimlerinde karmaşa yaratacak ve Principle of Least Privilege (En Az Yetki Prensibi) kuralını gevşetecekti. Bu sebeple "Ponytail Sadelik Merdiveni" ve YAGNI gereğince kullanılmayan tüm şemsiye yapılar ayıklandı; sistem %100 saf, öngörülebilir ve kaynak bazlı granüler PBAC mimarisine kavuşturuldu.

---

## 3. Hangi Tasarım Desenlerini (Pattern'leri) Kullandık?

```
+-----------------------------------------------------------------------------------+
|                            HYBRID RBAC + PBAC ARCHITECTURE                        |
|                                                                                   |
|  [ JWT Token with Claims: sub, role="Guest", permission="catalog:movies:read" ]   |
|                                         |                                         |
|                                         v                                         |
|  +-----------------------------------------------------------------------------+  |
|  |                     Catalog Minimal API Endpoints                           |  |
|  |                                                                             |  |
|  |  GET  /api/catalog/movies/{id}     --> CanReadCatalogMovies   (MoviesRead)  |  |
|  |  POST /api/catalog/movies          --> CanCreateCatalogMovies (MoviesCreate)|  |
|  |  PUT  /api/catalog/movies/{id}     --> CanUpdateCatalogMovies (MoviesUpdate)|  |
|  |  DELETE /api/catalog/movies/{id}   --> CanDeleteCatalogMovies (MoviesDelete)|  |
|  |  GET  /api/catalog/admin/media     --> CanReadAdminMedia      (Admin/Read)  |  |
|  +-----------------------------------------------------------------------------+  |
+-----------------------------------------------------------------------------------+
```

1. **Permission-Based Access Control (PBAC):**
   Statik rol kontrolleri yerine, istek yapan kullanıcının token'ındaki `permission` claim'leri üzerinden mikro düzeyde yetki kontrolü sağlandı.
2. **Single Source of Truth (SSOT):**
   Tüm izinler (`SystemPermissions`), roller (`SystemRoles`) ve politikalar (`SystemPolicies`) `ShuffleSeries.Shared.Core.Domain.Constants` içinde strongly-typed sabitler olarak toplandı. "Distributed Magic String Sprawl" önlendi.
3. **Idempotent Database Seeding:**
   `IdentityDbSeeder` veritabanındaki mevcut kayıtları `ToListAsync()` ile kontrol ederek yalnızca eksik olan izinleri ve rolleri ekler; var olan ilişkileri korur ve çoklu çalıştırmalarda hata üretmez.

---

## 4. Tanımlanan İzinler ve Rol Matrisi

### Granüler İzin Listesi
| Kaynak | İzin Kodu | Tanım |
| :--- | :--- | :--- |
| **Movies** | `catalog:movies:read`, `catalog:movies:create`, `catalog:movies:update`, `catalog:movies:delete` | Film detay okuma, ekleme, güncelleme ve silme |
| **Series** | `catalog:series:read`, `catalog:series:create`, `catalog:series:update`, `catalog:series:delete` | Dizi detay okuma, ekleme, güncelleme ve silme |
| **Episodes** | `catalog:episodes:read`, `catalog:episodes:create`, `catalog:episodes:update`, `catalog:episodes:delete` | Bölüm detay okuma, ekleme, güncelleme ve silme |
| **Platforms** | `catalog:platforms:read`, `catalog:platforms:create`, `catalog:platforms:update`, `catalog:platforms:delete` | Platform listeleme, ekleme, güncelleme ve silme |
| **Moods** | `catalog:moods:read`, `catalog:moods:create`, `catalog:moods:update`, `catalog:moods:delete` | Mod listeleme, ekleme, güncelleme ve silme |
| **Genres** | `catalog:genres:read`, `catalog:genres:create`, `catalog:genres:update`, `catalog:genres:delete` | Tür listeleme, ekleme, güncelleme ve silme |
| **Associations** | `catalog:associations:manage` | Platform, mood, tür bağı ve çeviri yönetimi |
| **Admin CMS** | `catalog:admin:read` | Yönetici paneli medya listesi ve CMS sorgulama |

### Rol - İzin Eşleşmesi
| Rol | İzinler |
| :--- | :--- |
| **Guest** | `catalog:movies:read`, `catalog:series:read`, `catalog:episodes:read`, `catalog:platforms:read`, `catalog:moods:read`, `catalog:genres:read`, `shuffle:basic` |
| **Standard** | `Guest` izinlerinin tamamı + `shuffle:basic` |
| **Premium** | `Standard` izinlerinin tamamı + `shuffle:vip` |
| **Admin** | Sistemdeki 28 iznin TAMAMI |

---

## 5. Nasıl Test Edilir? (Test Senaryoları & Rehber)

### Test Senaryosu 1: Yetkisiz İstek (401 Unauthorized)
Token olmadan korumalı bir katalog endpoint'ine istek atıldığında RFC 7807 ProblemDetails dönmelidir.
```bash
curl -i -X GET http://localhost:5000/api/catalog/movies/d3b07384-d113-4f44-8020-6d4a5c92c81a
```
**Beklenen Yanıt:**
```http
HTTP/1.1 401 Unauthorized
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc7235#section-3.1",
  "title": "Unauthorized",
  "status": 401,
  "detail": "Kimlik doğrulaması başarısız oldu. Geçerli bir Bearer token gereklidir.",
  "code": "AUTH_UNAUTHORIZED"
}
```

### Test Senaryosu 2: Eksik İzin ile İstek (403 Forbidden)
Kullanıcının geçerli bir token'ı var ancak yalnızca `shuffle:basic` iznine sahip (katalog okuma izni yok).
```bash
curl -i -X GET http://localhost:5000/api/catalog/movies/d3b07384-d113-4f44-8020-6d4a5c92c81a \
  -H "Authorization: Bearer <TOKEN_WITH_SHUFFLE_BASIC_ONLY>"
```
**Beklenen Yanıt:**
```http
HTTP/1.1 403 Forbidden
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.3",
  "title": "Forbidden",
  "status": 403,
  "detail": "Bu işlemi gerçekleştirmek için gerekli yetkiye veya izinlere sahip değilsiniz.",
  "code": "AUTH_FORBIDDEN"
}
```

### Test Senaryosu 3: Granüler `catalog:movies:read` İzni ile Başarılı İstek (200 OK)
Standart kullanıcının token'ında `catalog:movies:read` izni bulunduğunda film detayı başarıyla çekilir.
```bash
curl -i -X GET http://localhost:5000/api/catalog/movies/d3b07384-d113-4f44-8020-6d4a5c92c81a \
  -H "Authorization: Bearer <TOKEN_WITH_CATALOG_MOVIES_READ>"
```
**Beklenen Yanıt:**
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "id": "d3b07384-d113-4f44-8020-6d4a5c92c81a",
  "title": "Oppenheimer",
  "director": "Christopher Nolan",
  ...
}
```

### Test Senaryosu 4: Admin CMS Endpoint'ine Yetkili Erişim (200 OK)
Kullanıcı `Admin` rolüne VEYA `catalog:admin:read` iznine sahipse CMS filtreleme listesi döner.
```bash
curl -i -X GET "http://localhost:5000/api/catalog/admin/media?page=1&pageSize=10" \
  -H "Authorization: Bearer <ADMIN_OR_CMS_TOKEN>"
```
**Beklenen Yanıt:**
```http
HTTP/1.1 200 OK
{
  "items": [...],
  "pageNumber": 1,
  "totalPages": 1,
  "totalCount": 5
}
```

### Otomatik Test Paketi Komutları
```bash
# Tüm birim ve entegrasyon testlerini çalıştırma
dotnet test --no-build

# Yalnızca Identity Seeder testini çalıştırma
dotnet test ShuffleSeries.Identity/ShuffleSeries.Identity.Tests/ShuffleSeries.Identity.Tests.csproj --filter "FullyQualifiedName~IdentityDbSeederTests"

# Yalnızca Catalog Entegrasyon testlerini çalıştırma
dotnet test ShuffleSeries.Catalog/ShuffleSeries.Catalog.IntegrationTests/ShuffleSeries.Catalog.IntegrationTests.csproj
```
