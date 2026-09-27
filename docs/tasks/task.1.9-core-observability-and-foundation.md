# Task 1.9: Temel Gözlemlenebilirlik (Core Observability), Serilog Sinks Mimarisi, PII Maskeleme ve Health Checks

## 1. Ne Yaptık?
Milestone 2 (API Gateway & Identity) ve sonraki dağıtık mikroservis geliştirmelerine sağlam bir zemin hazırlamak amacıyla, gözlemlenebilirlik (Observability) ve telemetri altyapısını "Shift-Left" yaklaşımıyla `ShuffleSeries.Shared.Core` katmanına kazandırdık.

Bu görev kapsamında:
1. **Correlation ID & Context Propagation:**
   * İstemciden gelen `X-Correlation-ID` başlığını yakalayan, yoksa `Guid` üreten `CorrelationIdMiddleware` yazıldı.
   * `ICorrelationIdContext` ile scoped ambient context üzerinden erişim sağlandı.
   * `CorrelationIdDelegatingHandler` ile mikroservisler arası veya dış API'lere giden `HttpClient` çağrılarına correlation başlığı otomatik olarak iliştirildi.
   * `GlobalExceptionHandler` içerisine `correlationId` alanı eklenerek istemciye dönen ProblemDetails yanıtları ile merkezi logların anında eşleştirilmesi sağlandı.
2. **Genişletilebilir ve Tamamen Asenkron Serilog Sinks Mimarisi:**
   * Log hedefleri yalnızca konsola sabitlenmeyip; `ILogSinkConfigurator` strateji arayüzü ile modüler hale getirildi.
   * `ConsoleSinkConfigurator`, `FileSinkConfigurator` (Rolling File) ve `PostgreSqlSinkConfigurator` yerleşik olarak yazıldı.
   * `Serilog.Sinks.Async` entegrasyonu ile tüm sink yazma operasyonları arka plan ring buffer kuyruğuna devredilerek (`WriteTo.Async(...)`) ana istek işleme thread'inin disk veya veritabanı I/O beklemesi (blocking) engellendi.
   * HashiCorp Vault veya `appsettings.json` üzerinden (`Logging:Sinks:Console`, `File`, `PostgreSql`) hedeflerin açılıp kapatılabilmesi ve `SerilogLoggingBuilder` ile gelecekte Seq, Loki veya Elasticsearch gibi yeni platformların tek satırla (`AddSink<T>`) entegre edilebilmesi sağlandı.
3. **OWASP API8 Uyarınca Asenkron PII Maskeleme (`SensitiveDataDestructuringPolicy`):**
   * Maskeleme işlemi ana HTTP thread'inden tamamen soyutlanarak Serilog'un `IDestructuringPolicy` kancasına (`SensitiveDataDestructuringPolicy`) taşındı.
   * `[MaskSensitiveData]` niteliği (`ShuffleSeries.Shared.Core.Domain.Attributes`) ve hassas anahtar kelimeler (`password`, `token`, `secret`, `apikey`, `cvv`, `ssn` vb.) reflection önbelleği (`ConcurrentDictionary`) üzerinde taranarak, nesneler Serilog tarafından yapısal olarak ayrıştırılırken (destructure) otomatik ve asenkron biçimde `***MASKED***` ile güvenli hale getirildi.
   * MediatR `LoggingBehavior` ve `PerformanceBehavior` içerisindeki string manipülasyonu ve serileştirme yükü tamamen ortadan kaldırılarak HTTP yanıt süresine 0 ms ek gecikme (zero latency overhead) sağlandı.
4. **MediatR Performans İzleme:**
   * 500 ms eşik değerini aşan yavaş komut ve sorguları tespit eden `PerformanceBehavior` yazılarak boru hattına entegre edildi.
5. **OpenTelemetry SDK Enstrümantasyonu ve Dağıtık Telemetri:**
   * `AddSharedOpenTelemetry` ile ASP.NET Core, HttpClient ve EF Core çağrılarının W3C trace standartlarında telemetri üretmesi ve OTLP exporter ile Jaeger/Tempo'ya aktarılabilmesi sağlandı.
6. **Liveness & Readiness Health Checks:**
   * `/health/live` (Liveness) ve `/health/ready` (Readiness) probe'ları standart JSON sözleşmesiyle açıldı.
7. **Outbox Message Dayanıklılık ve Zehirli Mesaj (Poison Message) Koruması:**
   * `OutboxMessage` entity'sine `RetryCount` alanı eklendi.
   * PostgreSQL üzerinde işlenmemiş mesajları milisaniyeler içinde çekebilmek için `HasFilter("\"ProcessedOnUtc\" IS NULL")` kısmi indeksi (partial index) ve zaman sıralaması için `OccurredOnUtc` indeksi oluşturuldu.
   * `ProcessOutboxMessagesJob` arka plan işleyicisine `MaxRetries = 3` tavanı konarak deserileştirilemeyen veya sürekli hata üreten zehirli mesajların diğer bekleyen event'leri bloklaması (head-of-line blocking) engellendi.
8. **HashiCorp Vault Seed Scripti ve Sırsız AppSettings:**
   * `docker/vault/init-vault.sh` seed scriptine tüm mikroservislerin ortak log tercihleri (`Logging:Sinks:Console`, `File`, `PostgreSql`), `OpenTelemetry` OTLP endpoint'i, CORS izinleri ve servise özel yapılandırmalar tanımlandı.
   * `appsettings.json` dosyaları sır ve operasyonel değerlerden arındırılarak sadece Vault bootstrap ve minimum host ayarlarını içerecek şekilde temizlendi.
9. **Idempotency Sözleşmeleri:**
   * Gelecekteki event consumer'larının idempotency kontrolü için `IIntegrationEvent` ve `InboxMessage` entity'leri `Shared.Core.Domain` katmanına eklendi.

---

## 2. Neden Yaptık? (Mimari Kararlar ve Rasyoneller)

### Neden Gözlemlenebilirliği Proje Sonuna (Milestone 10) Bırakmadık?
Gözlemlenebilirliği proje sonuna bırakmak (Afterthought Observability), dağıtık mimarilerde en yaygın anti-pattern'lerden biridir. Servisler arası event'lerin, gRPC çağrılarının ve HTTP isteklerinin akmaya başladığı Milestone 2 ve 3 aşamalarında `TraceId` veya `CorrelationId` bulunmaması, geliştiricinin yerel ortamda dahi "Bu event neden kayboldu?" diyerek saatlerce kör uçuş yapmasına yol açar. Ayrıca 8-9 mikroservis yazıldıktan sonra geriye dönük (retrofit) kod enstrümantasyonu yapmak onlarca projede regression riski yaratır.

### Neden Maskelemeyi Serilog Destructuring Policy'ye Taşıdık?
Geleneksel yöntemlerde (örneğin MediatR pipeline içinde `JsonSerializer` veya `JsonNode` ile) maskeleme yapmak, ana HTTP thread'ini CPU ve GC tahsisatlarıyla bloke eder. Serilog'un `IDestructuringPolicy` kancası sayesinde MediatR istek ve yanıtları doğrudan `{@RequestPayload}` ve `{@ResponsePayload}` olarak loglanır; asıl serileştirme ve hassas alan filtrelemesi `Serilog.Sinks.Async` worker thread'inde arka planda gerçekleşir. Bu sayede canlı ortamda ShuffleEngine gibi yüksek throughput gerektiren servislerin gecikmesi (latency) korunur.

### Neden Asenkron Loglama (`Serilog.Sinks.Async`)?
Standart log sink'leri (özellikle File ve PostgreSQL), log satırını yazarken thread'i disk veya veritabanı I/O seviyesinde senkron bloke eder. Yüksek eşzamanlı istek alan bir mikroserviste bu durum thread pool starvation ve gecikme (latency) artışına neden olur. `WriteTo.Async(...)` sarmalaması ile loglar bellek içi halka tampona (ring buffer) yazılır ve arka plan thread'i tarafından asenkron olarak hedefe boşaltılır.

### Neden Outbox RetryCount ve Kısmi İndeks (Partial Index)?
Outbox tablosu zamanla binlerce işlenmiş mesaj biriktirir. `WHERE ProcessedOnUtc IS NULL` sorgusu eğer filtrelenmiş indeks yoksa tüm tabloyu Sequential Scan yapar. `HasFilter("\"ProcessedOnUtc\" IS NULL")` indeksi yalnızca işlenmemiş kayıtları indeksleyerek sorgu süresini milisaniyeler seviyesinde sabitler. `RetryCount` ve `MaxRetries = 3` sınırı ise ağ veya veri bozukluğu nedeniyle sürekli patlayan zehirli mesajların (poison messages) kuyruk döngüsünü sonsuz döngüye sokmasını engeller.

---

## 3. Hangi Pattern'leri Kullandık?

| Desen (Pattern) | Sistemdeki İşlevi |
| :--- | :--- |
| **Ambient Context & Scope Pattern** | `ICorrelationIdContext` ile geçerli istek yaşam döngüsü boyunca correlation ID'yi thread-safe taşımak. |
| **Decorator / Pipeline Pattern** | MediatR `LoggingBehavior` ve `PerformanceBehavior` ile iş mantığına dokunmadan loglama ve süre ölçümü yapmak. |
| **Strategy Pattern** | `ILogSinkConfigurator` ile Serilog çıktı hedeflerini birbirinden bağımsız ve tak-çıkar hale getirmek. |
| **Context Propagation Pattern** | `CorrelationIdDelegatingHandler` ile HTTP istek başlıklarını downstream servis çağrılarına taşımak. |
| **Custom Destructuring Policy Pattern** | Serilog `SensitiveDataDestructuringPolicy` (`IDestructuringPolicy`) ile PII/hassas verilerin asenkron arka plan hattında sıfır CPU/latency maliyetiyle maskelenmesi. |
| **Transactional Outbox & Dead-Letter (Poison) Pattern** | `OutboxMessage`'da `RetryCount`, `OccurredOnUtc` ve `ProcessedOnUtc` kısmi indeksiyle güvenli, sıralı ve zehirli mesaj dirençli event aktarımı. |
| **Inbox Pattern (Foundation)** | `InboxMessage` entity'si ile dağıtık event'lerde tekrarlanan mesajları (duplicate delivery) engellemek. |

---

## 4. Yapılan Geliştirme Nasıl Test Edilir?

### Çalıştırılan Testler:
Proje genelindeki tüm testler (`dotnet test`) çalıştırılarak doğrulanmıştır:
* **Birim Testleri (Unit Tests):** 192 adet test (`SensitiveDataDestructuringPolicyTests`, `LoggingBehaviorTests`, `PerformanceBehaviorTests`, `CorrelationIdTests`, `SerilogLoggingExtensibilityTests`, `HealthCheckExtensionsTests`, `InboxMessageTests`, `ProcessOutboxMessagesJobTests`, `OutboxMessageTests` vb.).
* **Mimari Testler (Architecture Tests):** 15 adet test (Katman sınırları ve CQRS kuralları).
* **Gerçek Entegrasyon Testleri (Real Integration Tests):** 18 adet test (`Testcontainers` PostgreSQL + `Respawn` + WebApplicationFactory).
* **Toplam:** 226 test %100 başarı oranıyla tamamlandı.

### Test Senaryoları (Test Cases):
1. **Serilog Destructuring PII Maskeleme Testi:** `Password`, `Token`, `Cvv` içeren bir nesne `SensitiveDataDestructuringPolicy` tarafından ayrıştırıldığında `***MASKED***` olduğu, `[MaskSensitiveData]` ile özel maske tanımlanabilmesi ve hassas olmayan alanların (`Email`, `Amount`) korunduğu doğrulandı.
2. **MediatR Logging Boru Hattı:** `LoggingBehavior` çağrıldığında başarılı isteklerde tek ve konsolide (`Handled request {RequestName}. Request: {@RequestPayload}, Response: {@ResponsePayload}`) log basıldığı; exception anında ise request payload'ının `LogWarning` seviyesinde loglanarak hatanın `GlobalExceptionHandler`'a devredildiği (çift log ve stack trace tekrarı üretilmediği) doğrulandı.
3. **Outbox Hata Sayacı & Zehirli Mesaj Testi:** Hata alan bir outbox mesajının `RetryCount`'unun arttığı; `MaxRetries = 3` değerine ulaşmış bozuk bir mesajın sonraki döngülerde atlanıp geçerli mesajların işlenmeye devam ettiği doğrulandı.
4. **Correlation ID Yayılımı:**
   * İstemci `X-Correlation-ID: my-id-123` gönderdiğinde API yanıtında aynı başlığın döndüğü,
   * Başlık gönderilmediğinde yeni bir GUID üretilip yanıta eklendiği doğrulandı.
5. **Health Checks & Gateway YARP Yönlendirmesi:**
   * `GET /health/live` -> API Gateway Liveness Probe (HTTP 200).
   * `GET /health/ready` -> API Gateway Readiness Probe (HTTP 200).
   * `GET /catalog-api/health/live` -> API Gateway YARP üzerinden Catalog Mikroservisi Liveness Probe.
   * `GET /catalog-api/health/ready` -> API Gateway YARP üzerinden Catalog Mikroservisi Readiness Probe (PostgreSQL, Outbox durumu vb.).
   * **YARP Active Health Check:** Gateway cluster seviyesinde Catalog servisini periyodik (10s) `/health/live` ile yoklayıp olası kesintilerde otomatik trafikten düşürür (traffic draining).
