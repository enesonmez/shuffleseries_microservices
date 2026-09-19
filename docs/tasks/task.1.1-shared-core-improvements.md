# Task 1.1: Shared.Core Geliştirmeleri

Bu doküman, ShuffleSeries mikroservis ekosisteminin ortak çekirdeği olan `ShuffleSeries.Shared.Core` kütüphanesinde yapılan geliştirmeleri, mimari kararları, uygulanan tasarım desenlerini ve test senaryolarını detaylandırır.

---

## 1. Ne Yaptık?
1. **Temel Varlık Yapısının Genişletilmesi (`BaseEntity` & `Entity`):**
   - Tüm servislerin kullanabileceği `BaseEntity<TId>` generic sınıfı ve Guid tabanlı `BaseEntity` sınıfı geliştirildi.
   - Mevcut `Entity` sınıfı `BaseEntity`'den türetilerek `Catalog` mikroservisi ve diğer olası bağımlılıklarla geriye dönük tam uyumluluk sağlandı.
   - Denetim (Audit) alanları (`CreatedAtUtc`, `CreatedBy`, `ModifiedAtUtc`, `ModifiedBy`, `DeletedAtUtc`, `DeletedBy`) standartlaştırıldı; nullability düzeltmeleri yapıldı.
   - Kimlik bazlı eşitlik karşılaştırmaları (`IEquatable<BaseEntity<TId>>`, `==`, `!=`, `GetHashCode`) tanımlandı.

2. **Merkezi İstisna Hiyerarşisi (`CustomException`):**
   - HTTP durum kodu, hata kodu (`Code`) ve başlık (`Title`) taşıyan soyut `CustomException` temel sınıfı oluşturuldu.
   - `BusinessException` (422 Unprocessable Entity), `NotFoundException` (404 Not Found) ve `ValidationException` (400 Bad Request) `CustomException`'dan türetildi.
   - Dağıtık mimaride sıklıkla ihtiyaç duyulan `ConflictException` (409 Conflict), `UnauthorizedException` (401 Unauthorized), `ForbiddenException` (403 Forbidden) ve `InternalServerException` (500 Internal Server Error) sınıfları eklendi.

3. **Global ProblemDetails İstisna Yönetimi (`GlobalExceptionHandler`):**
   - ASP.NET Core `IExceptionHandler` arayüzü RFC 7807 ve RFC 9457 standartlarına göre zenginleştirildi.
   - `ValidationException` durumunda validasyon hataları `errors` extension alanına taşındı.
   - `CustomException` türevleri polimorfik olarak ele alınarak her istisnanın tanımladığı `StatusCode`, `Code`, `Title` ve RFC Type değerleri yanıta aktarıldı.
   - Dağıtık sistemlerde uçtan uca izlenebilirlik için `Activity.Current?.Id ?? httpContext.TraceIdentifier` değeri `traceId` extension alanı olarak eklendi.
   - Güvenlik (OWASP API8) gereği 500 Internal Server Error durumlarında istemciye stack trace ve iç veri sızdırılmayacak şekilde genel mesaj dönecek izolasyon sağlandı.
   - `Content-Type: application/problem+json` başlığı garanti altına alındı.

4. **Ortak DTO ve Yanıt Yapıları:**
   - `PaginatedList<T>` sınıfına `[JsonConstructor]` ve eşleşen parametre adlandırması eklenerek istemcilerin ve mikroservislerin JSON deserialization yapabilmesi sağlandı.
   - Sayfalama istekleri için otomatik normalizasyon ve üst sınır koruması sağlayan `PaginationRequest` DTO'su oluşturuldu.
   - Standart yanıt zarfı (response envelope) olarak `ApiResponse<T>` ve `ApiResponse` sınıfları eklendi.

5. **Kapsamlı Birim Testleri:**
   - `ShuffleSeries.Shared.Core.Tests` xUnit test projesi çözüme eklendi. Varlık eşitliği, hata yönetimi, JSON serileştirme ve sayfalama davranışları 30 birim test ile doğrulandı.

---

## 2. Neden Yaptık?
- **Tutarlılık (Consistency across Microservices):** Dağıtık bir mimaride her servisin kendi hata formatını veya entity yapısını tanımlaması istemci (API Gateway, mobil uygulama, frontend) tarafında entegrasyon karmaşasına yol açar. Shared.Core ile tüm servisler aynı RFC uyumlu hata şemasını ve ortak sözleşmeleri paylaşır.
- **İzlenebilirlik (Observability & Traceability):** Hata yanıtlarına `traceId` eklenmesi, canlı ortamda bir kullanıcının karşılaştığı hatayı Jaeger veya merkezi log sistemlerinde (Loki/Seq) milisaniyeler içinde bulmayı mümkün kılar.
- **Güvenlik (Security by Design):** OWASP API Security kuralları gereği, veritabanı bağlantı hataları veya beklenmeyen kod kırılmalarında stack trace bilgisi saldırganlara sistem topolojisi hakkında bilgi verir. Standart ProblemDetails filtresi bu bilgileri maskeler.
- **Serialization Güvenilirliği:** Mikroservisler arası HTTP/REST çağrılarında veya event mesajlarında `PaginatedList<T>` gibi modellerin kayıpsız deserialize edilebilmesi için constructor parametre eşleşmesi şarttır.

---

## 3. Hangi Pattern'leri Kullandık?
- **Layered Supertype Pattern:** `BaseEntity<TId>` ve `CustomException`, tüm domain varlıkları ve istisnaları için ortak özellikleri tek merkezde toplayarak DRY (Don't Repeat Yourself) prensibini uyguladı.
- **Identity Field & Value Equality Pattern (DDD):** Varlıklar referans adreslerine göre değil, benzersiz kimliklerine (`Id`) ve tiplerine göre eşit kabul edilir. `IEquatable` ve operatör aşırı yüklemeleri ile uygulandı.
- **Polymorphic Exception Handling (Middleware / Filter Pattern):** `GlobalExceptionHandler`, fırlatılan hatanın türüne göre davranışı polimorfik olarak belirler. Yeni bir `CustomException` eklendiğinde middleware kodunu değiştirmeye gerek kalmadan doğru HTTP status ve ProblemDetails üretilir (Open/Closed Principle).
- **Envelope Pattern:** `ApiResponse<T>` ve `PaginatedList<T>`, ham veriyi metadata (başarı durumu, mesajlar, sayfalama indeksleri) ile sarmalayarak istemciye zengin bağlam sunar.

---

## 4. Yapılan Geliştirme Nasıl Test Edilir?

### Test Komutları
Çözüm dizininde aşağıdaki komut çalıştırılarak tüm birim testler koşturulur:
```bash
dotnet test
```

Yalnızca `Shared.Core` testlerini çalıştırmak için:
```bash
dotnet test --filter "FullyQualifiedName~ShuffleSeries.Shared.Core.Tests"
```

### Öne Çıkan Test Senaryoları (Test Cases)
| Test Adı | Amaç | Beklenen Sonuç |
| :--- | :--- | :--- |
| `Constructor_WithId_ShouldSetIdAndCreatedAtUtc` | Varlık oluşturulduğunda Id ve zaman damgası atanması | `Id` atanır, `CreatedAtUtc` şu anki zamana yakın olur, denetim alanları başlangıçta null kalır. |
| `Equals_WithSameIdAndType_ShouldReturnTrue` | Aynı Id ve tipe sahip iki nesnenin eşitliği | `entity1 == entity2` ve `Equals` `true` döner, `GetHashCode` değerleri eşleşir. |
| `Equals_WithDifferentType_ShouldReturnFalse` | Aynı Id ancak farklı sınıflara ait nesneler | `false` döner. |
| `TryHandleAsync_ValidationException_ShouldReturn400WithErrors` | FluentValidation doğrulaması başarısız olduğunda | HTTP 400, `Validation Error`, `errors` sözlüğü ve `traceId` içeren `application/problem+json`. |
| `TryHandleAsync_NotFoundException_ShouldReturn404` | Kaynak bulunamadığında | HTTP 404, `Not Found` başlığı ve entity'ye özel hata kodu (`MOVIE_NOT_FOUND`). |
| `TryHandleAsync_UnhandledException_ShouldReturn500WithGenericMessage` | Beklenmedik iç sistem hatasında | HTTP 500, hassas veri maskelenmiş genel mesaj ("An unexpected error occurred on the server."). |
| `PaginatedList_ShouldSerializeAndDeserializeJsonCorrectly` | JSON serileştirme ve geri dönüştürme | `TotalCount`, `PageNumber`, `Items` kayıpsız şekilde nesneye dönüştürülür. |
| `PaginationRequest_ShouldNormalizeValues` | Hatalı veya sınır dışı sayfalama istekleri | Negatif değerler 1 ve 10'a, 100'den büyük boyutlar 100'e otomatik düzeltilir. |
