Bu doküman, ShuffleSeries projesinin uçtan uca geliştirme sürecini aşamalara (Milestones) ve görevlere (Tasks) bölerek detaylandırır. Proje; güvenli, ölçeklenebilir, yüksek performanslı ve "Senior" seviye mimari pratiklere (DDD, CQRS, Event-Driven, Observability) uygun olarak inşa edilecektir.

# Milestone 1: Altyapı, Çekirdek (Shared.Core) ve CI/CD Kurulumu
Mikroservislerin üzerinde yükseleceği ortak yapıların ve dağıtım kanallarının kurulması.
* [x] Task 1.1: Shared.Core Geliştirmeleri
  * Tüm servislerde kullanılacak BaseEntity, CustomException ve ortak DTO'ların oluşturulması.
  * Global Exception Handling Middleware ve standart ProblemDetails yanıt yapısının kurulması.
* [ ] Task 1.2: Soft Delete Altyapısı
  * Entity'lere IsDeleted flag'i eklenerek EF Core seviyesinde Global Query Filter (Interceptor veya Base DbContext) ile Soft Delete mantığının entegre edilmesi.
* [ ] Task 1.3: Local Development Altyapısı (Docker Compose)
  * Geliştirme ortamında kullanılacak PostgreSQL, MongoDB, Redis, Elasticsearch ve RabbitMQ'nun yapılandırılarak tek bir docker-compose.yml dosyası ile ayağa kaldırılması. (Not: Mikroservis Dockerfile'ları burada değil, her servis geliştirildikçe kendi task'i içinde yazılacaktır).
* [ ] Task 1.4: Secret Management (Vault) Entegrasyonu
  * HashiCorp Vault altyapısının docker-compose ortamına servis olarak eklenmesi ve ayağa kaldırılması.
  * Shared.Core içerisine, mikroservisler ayağa kalkarken (Bootstrap aşamasında) konfigürasyonları doğrudan Vault üzerinden güvenli bir şekilde okuyacak .NET Configuration Provider entegrasyonunun yazılması.
  * Veritabanı connection string'leri, JWT Secret Key, RabbitMQ ve Redis şifreleri gibi tüm hassas verilerin kaynak koddan ve konfigürasyon dosyalarından arındırılarak tamamen Vault içerisine taşınması.
* [ ] Task 1.5: CI/CD Pipeline (GitHub Actions)
  * Proje iskeleti için temel Build ve Test pipeline'ının oluşturulması.
  * SonarQube entegrasyonu ile kod kalite analizinin pipeline'a eklenmesi.

# Milestone 2: API Gateway ve Identity (Auth) Service
Kullanıcı girişlerinin ve sistem trafiğinin yönetilmesi.
* [ ] Task 2.1: YARP API Gateway Kurulumu
  * YARP tabanlı Gateway projesinin oluşturulması.
  * Mobil uygulamalardan gelecek yüksek istekleri (özellikle auth ve shuffle endpoint'lerini) korumak için Rate Limiting konfigürasyonlarının yapılması.
  * Gateway seviyesinde JWT Validasyonu (Authentication) yapısının kurulması.
  * Role & Claims bazlı Yetkilendirme (Authorization) politikalarının YARP route'larına entegre edilmesi (Böylece geçersiz token'a veya yetkisiz claim'e sahip istekler iç servislere hiç ulaşmadan Gateway'den döner).
* [ ] Task 2.2: Identity Service Geliştirmesi
  * POST /api/auth/register: Yeni kullanıcı kaydı.
  * POST /api/auth/login: Credential doğrulaması ve JWT + Refresh Token dönülmesi.
  * POST /api/auth/refresh: Süresi dolan JWT'nin refresh token ile yenilenmesi.
  * POST /api/auth/revoke: Kullanıcı çıkış yaptığında token'ın geçersiz kılınması.
* [ ] Task 2.3: Güvenlik ve Token Blacklist Mimarisi
  * Revoke edilen (kullanımdan kalkan) token'lar için Redis üzerinde bir Blacklist tutulması.
  * API Gateway JWT doğrularken bu Redis Blacklist'ini kontrol etmesinin (Security & Caching) sağlanması.

# Milestone 3: Catalog Service (The Source of Truth)
İçeriklerin (film, dizi, bölüm) ana yönetim merkezinin CQRS ve Event-Driven prensiplerle inşası.
* [ ] Task 3.1: Catalog CRUD İşlemleri (Write Tarafı)
  * POST / PUT / DELETE /api/catalog/movies: Film yönetimi.
  * POST / PUT / DELETE /api/catalog/series: Dizi yönetimi.
  * POST / PUT / DELETE /api/catalog/series/{seriesId}/episodes: Diziye bölüm ekleme/çıkarma.
  * "Black Mirror", "Love Death & Robots" gibi bağımsız bölümlü diziler için IsStandalone flag'inin eklenmesi.
  * Silme işlemlerinde Milestone 1'de kurulan Soft Delete altyapısının kullanılması.

* [ ] Task 3.2: Outbox Pattern ve RabbitMQ Entegrasyonu
  * Dağıtık sistem tutarsızlığını önlemek için MsSQL yazma işlemi ile event fırlatma işlemini tek transaction'da garantiye alacak Outbox Pattern'in kurulması.
  * Veri eklendiğinde/güncellendiğinde/silindiğinde MediaCreatedEvent, MediaUpdatedEvent ve MediaDeletedEvent mesajlarının RabbitMQ'ya atılması.

# Milestone 4: Shuffle Engine Service (The Core Engine)
Sistemin en çok trafik alacak ana damarının performans ve esneklik odaklı geliştirilmesi.
* [ ] Task 4.1: Event-Driven Veri Senkronizasyonu ve Data Seeding
  * RabbitMQ üzerinden fırlatılan MediaCreatedEvent, MediaUpdatedEvent ve MediaDeletedEvent mesajlarının dinlenerek Redis cache'inin anlık olarak güncellenmesi.
  * InBox Pattern kullanılarak aynı event'in iki kez işlenmesinin (Idempotency) önüne geçilmesi.
  * Redis'in boş olması (Cold Start) durumunda, Shuffle Engine'in gRPC üzerinden Catalog servisine bağlanarak başlangıç verisini (Bulk) çekmesi ve Redis'i doldurması (Cache Warming).
* [ ] Task 4.1: Redis In-Memory Altyapısı
  * Veritabanı (SQL) sorgularının tamamen devre dışı bırakılıp, metadata verisinin senkronize şekilde Redis (Sets veya Sorted Sets) üzerinden Memory'de yönetilmesi.
* [ ] Task 4.2: Shuffle Endpoint'lerinin Yazılması
  * GET /api/shuffle/movies?genre={genre}&year={year}: Rastgele film getirme.
  * GET /api/shuffle/series?genre={genre}: Rastgele dizi getirme.
  * GET /api/shuffle/episodes?seriesId={id}: Bağımsız bölümlü diziler için rastgele bölüm getirme.
* [ ] Task 4.3: Performans (Heap Allocation Minimizasyonu)
  * Performanslı array manipülasyonları için C#'ın Span<T> ve Memory<T> yapılarının kullanılması (GC yükünün azaltılması).
  * Cache Stampede (Yığılma) problemine karşı Locking veya Background Cache Warming stratejilerinin uygulanması.
* [ ] Task 4.4: Polly ile Fallback (Circuit Breaker)
  * Redis çökerse veya yanıt veremezse, Polly kullanılarak sistemin geçici olarak "Günün Popülerleri" (statik liste) dönmesini sağlayacak Fallback ve Circuit Breaker mekanizmasının kurulması.

# Milestone 5: Search Service
Elasticsearch kullanarak hızlı ve Event-Driven metin arama sisteminin kurulması.
* [ ] Task 5.1: Elasticsearch Entegrasyonu ve Endpoint'ler
  * GET /api/search/suggest?q={text}: Otomatik tamamlama (Edge N-Gram tokenizers kullanımı).
  * GET /api/search?q={text}&type={movie|series}: Detaylı arama sonuçları.
* [ ] Task 5.2: Eventual Consistency ve InBox Pattern
  * RabbitMQ'dan MediaCreatedEvent, MediaUpdatedEvent ve MediaDeletedEvent mesajlarının dinlenerek Elastic indeksinin güncellenmesi için Consumer'ların yazılması. (Nihai Tutarlılık).
  * İki kere işlenmeyi engellemek için Idempotency sağlayan InBox Pattern uygulanması.

# Milestone 6: Profile & Preferences Service
Kullanıcının uygulamadaki "kişiliğini" (sevdiği türler, engellediği aktörler) yönetecek esnek yapının kurulması.
* [ ] Task 6.1: Dinamik Veri Yönetimi
  * JSONB destekli PostgreSQL veritabanının kurgulanması.
  * [ ] Task 6.2: Kullanıcı Tercihleri Endpoint'leri
  * GET /api/profile/me: Profil ve tercihleri (favoriler/engellenenler) listeleme.
  * PUT /api/profile/me: Avatar/biyografi/genel ayarları güncelleme.
  * POST /api/profile/preferences/genres: Sevilen/İstenmeyen (include/exclude) türleri ekleme.
  * DELETE /api/profile/preferences/genres/{genreId}: Tür tercihini çıkarma.
  * POST /api/profile/preferences/actors: Aktör engelleme veya favoriye alma.
* [ ] Task 6.3: Shuffle Engine İletişimi (gRPC / Redis)
  * Shuffle Engine'in tercihleri milisaniyeler içinde okuyabilmesi için gRPC entegrasyonu veya ortak Redis kümesi üzerinden (Asenkron) tercih senkronizasyonunun yapılması.
  * Profil değiştiğinde RabbitMQ'ya ProfileUpdatedEvent atılarak Shuffle Engine'deki pre-computed (hazırda bekleyen) önbelleğin temizlenmesi (Cache Invalidation).

# Milestone 7: History, Analytics & Notification Servisleri
Kullanıcı hareketlerinin takibi ve mobil cihazlarla etkileşim.
* [ ] Task 7.1: History & Analytics Service (MongoDB)
  * POST /api/history/watch-action: İzleme kaydı (202 Accepted dönüp Task.Run/Message Queue ile asenkron DB yazımı).
  * POST /api/history/shuffle-action: Swipe/skip kaydının tutulması (Gelecekteki AI özellikleri için).
  * GET /api/history/users/me: İzleme geçmişi sorgulama.
  * MongoDB'de verilerin UserId'ye (Shard Key) göre Sharding kurgusu ile dağıtılması.
* [ ] Task 7.2: Notification (Push & Email) Service Core
  * POST /api/notifications/devices: Firebase Device Token (iOS/Android) kaydı.
  * GET /api/notifications/me & PUT /api/notifications/{id}/read: Uygulama içi bildirim zili/geçmişi.
  * E-posta gönderim altyapısının (SMTP, SendGrid, Mailgun vb.) Notification servisine entegre edilmesi.
* [ ] Task 7.3: Event-Driven Push & Email Notification Flow
  * NewMediaAddedEvent mesajının dinlenmesi ve gRPC üzerinden Profile servisinden "Bilim Kurgu seven ve bildirimi açık" kullanıcıların ID'lerinin sorgulanıp Push atılması.
  * Identity servisinden fırlatılan UserRegisteredEvent mesajının dinlenmesi (Subscribe) ve yeni kayıt olan kullanıcıya "Hoş Geldin" e-postasının asenkron olarak gönderilmesi.
  * Gönderilen mesajın (Push veya Email) Hash/Event ID'sinin Redis'te tutularak Idempotency (aynı bildirimi/maili yanlışlıkla iki kez atmama) sağlanması.
  * Firebase/APNs veya E-posta sunucusu çökmelerine/gecikmelerine karşı Polly Exponential Backoff kullanılması ve başarısız mesajların DLQ (Dead Letter Queue) mekanizmasına alınması.

# Milestone 8: Gözlemlenebilirlik (Observability) ve Monitoring
Sistemin "kör uçuş" yapmasını engelleyecek altyapıların entegrasyonu.
* [ ] Task 8.1: Metrics (Metrikler)
  * Servislere .NET Health Checks ve OpenTelemetry entegrasyonu.
  * /metrics endpoint'i açılarak CPU, RAM, HTTP 5xx hata oranları ve gecikmelerin Prometheus ile toplanması (pull).
  * Prometheus üzerinden pull edilen verilerle Grafana Dashboard'larının hazırlanması.
* [ ] Task 8.2: Distributed Tracing (Dağıtık İzleme)
  * OpenTelemetry ve Jaeger entegrasyonu ile mikroservisler arası isteklerin Trace ID ile uçtan uca (örn: Gateway -> Search -> Elasticsearch) takip edilmesi.
* [ ] Task 8.3: Centralized Logging (Merkezi Loglama)
  * Tüm servislerde logların konsola değil, yapılandırılmış (Structured JSON) log olarak Serilog aracılığıyla merkezi bir Seq veya Loki sistemine (TraceID ve UserId ile) aktarılması.
  * Request - Response loglaması API Gateway katmanında merkezi bir yapıda kişisel verilerin maskelenerek loglanması.