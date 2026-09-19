# ShuffleSeries Architectural Lessons & Engineering Knowledge Base

Bu doküman, ShuffleSeries mikroservis ekosisteminde geliştirme, hata çözümü ve refactoring süreçlerinde edinilen mimari tecrübeleri ve kritik tasarım kurallarını kayıt altına alır. Her madde dikey uzmanlık alanına göre etiketlenmiştir.

---

### [Architecture / Domain Modeling]
* **BaseEntity & Entity Uyumluluğu:** Mikroservis bounded context'lerinde `AggregateRoot` ve bağımsız varlıklar tanımlanırken, generic `BaseEntity<TId>` tabanı ile Guid varsayılanlı `BaseEntity` ve `Entity` türevleri birlikte sağlanmalıdır. Bu, hem MongoDB / NoSQL gibi string Id kullanan servisleri hem de PostgreSQL kullanan servisleri tek çatı altında destekler ve geriye dönük uyumluluk kırılmalarını (breaking changes) önler.
* **Audit Alanları Nullability Kuralı:** Yeni oluşturulan bir varlıkta `CreatedAtUtc` zorunlu ve non-null iken; `ModifiedAtUtc`, `ModifiedBy`, `DeletedAtUtc` ve `DeletedBy` alanları mutlaka nullable (`DateTime?`, `string?`) olmalıdır. Aksi halde ilişkisel veritabanlarında `0001-01-01` (min value) gibi geçersiz zaman damgaları kaydedilerek UTC doğrulama hatalarına yol açar.

### [Web & API / Security (OWASP)]
* **IExceptionHandler ve ProblemDetails Content-Type Kuralı:** ASP.NET Core `HttpResponse.WriteAsJsonAsync(...)` varsayılan çağrıldığında `Content-Type` değerini `application/json; charset=utf-8` olarak ezer. RFC 9457 ve RFC 7807 standartlarına uygun bir `application/problem+json` yanıtı dönebilmek için `WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json", cancellationToken)` aşırı yüklemesi açıkça belirtilmeli ve `Response.StatusCode` atanmalıdır.
* **TraceId ve Gözlemlenebilirlik:** ProblemDetails yanıtlarına W3C standardında `Activity.Current?.Id ?? httpContext.TraceIdentifier` üzerinden `traceId` extension alanı eklenmelidir. Bu sayede API Gateway ve mikroservisler arasında dağıtık izleme (Distributed Tracing - Jaeger/OpenTelemetry) uçtan uca hata ayıklama sağlar.
* **Güvenli Hata İzolasyonu (OWASP API8/Information Disclosure):** 500 Internal Server Error durumlarında kullanıcıya veya istemciye asla `exception.StackTrace` veya iç sistem hata detayları iletilmemeli; standart ve genel ("An unexpected error occurred on the server.") bir hata mesajı dönülmeli, asıl exception ise yapılandırılmış (structured) loglar aracılığıyla izole edilmelidir.

### [Application & Serialization]
* **System.Text.Json Deserialization ve Constructor Parametreleri:** `PaginatedList<T>` gibi read-only özelliklere sahip immutable DTO sınıflarında, constructor parametre isimleri özellik isimleriyle harfi harfine eşleşmelidir (örn. `totalCount` -> `TotalCount`). Aksi halde `System.Text.Json` deserialization sırasında eşleşme sağlayamaz ve değerleri varsayılan (0/null) olarak atar. Sınıfa açıkça `[JsonConstructor]` eklenmesi en güvenli yaklaşımdır.
* **Sayfalama DTO'larında Sınır Güvenliği:** `PaginationRequest` gibi sorgu DTO'larında sayfa numarası en az 1, sayfa boyutu ise mantıklı bir üst sınırla (örn. MaxPageSize = 100) constructor seviyesinde sınırlandırılmalıdır. Bu, veritabanı seviyesinde beklenmeyen bellek tükenmelerini (OOM / DoS) önler.
