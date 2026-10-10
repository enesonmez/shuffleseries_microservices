Bu doküman, ShuffleSeries projesinin uçtan uca geliştirme sürecini aşamalara (Milestones) ve görevlere (Tasks) bölerek detaylandırır. Proje; güvenli, ölçeklenebilir, yüksek performanslı ve "Senior" seviye mimari pratiklere (DDD, CQRS, Event-Driven, Observability) uygun olarak inşa edilecektir.

# Milestone 1: Altyapı, Çekirdek (Shared.Core) ve CI/CD Kurulumu
Mikroservislerin üzerinde yükseleceği ortak yapıların ve dağıtım kanallarının kurulması.
* [x] Task 1.1: Shared.Core Geliştirmeleri
  * Tüm servislerde kullanılacak BaseEntity, CustomException ve ortak DTO'ların oluşturulması.
  * Global Exception Handling Middleware ve standart ProblemDetails yanıt yapısının kurulması.
* [x] Task 1.2: Soft Delete Altyapısı
  * Entity'lere IsDeleted flag'i eklenerek EF Core seviyesinde Global Query Filter (Interceptor veya Base DbContext) ile Soft Delete mantığının entegre edilmesi.
* [x] Task 1.3: Local Development Altyapısı (Docker Compose)
  * Geliştirme ortamında kullanılacak PostgreSQL, MongoDB, Redis, Elasticsearch ve RabbitMQ'nun yapılandırılarak tek bir docker-compose.yml dosyası ile ayağa kaldırılması. (Not: Mikroservis Dockerfile'ları burada değil, her servis geliştirildikçe kendi task'i içinde yazılacaktır).
* [x] Task 1.4: Secret Management (Vault) Entegrasyonu
  * HashiCorp Vault altyapısının docker-compose ortamına servis olarak eklenmesi ve ayağa kaldırılması.
  * Shared.Core içerisine, mikroservisler ayağa kalkarken (Bootstrap aşamasında) konfigürasyonları doğrudan Vault üzerinden güvenli bir şekilde okuyacak .NET Configuration Provider entegrasyonunun yazılması.
* [x] Task 1.5: Swagger altyapısı kurulumu
  * Microsoft.AspNetCore.OpenApi (.NET 10 yerel motoru) ile OpenAPI v3 dokümantasyon altyapısının Shared.Core.Web içerisine kurulması.
  * Klasik Swagger UI (/swagger) ve modern Scalar API Reference (/scalar/v1) arayüzlerinin tek merkezden entegrasyonu.
  * IOpenApiDocumentTransformer ile otomatik JWT Bearer (Authorize) yetkilendirme şemasının OpenAPI dokümanına enjekte edilmesi.
  * Catalog.Api Minimal API endpoint'lerinin zengin OpenAPI metadata'ları (özet, açıklama, tag, typed status codes) ile dokümante edilmesi.
* [x] Task 1.6: CI/CD Pipeline (GitHub Actions)
  * GitHub Actions üzerinde .NET 10 ve Java 21 runtime'ları ile çok aşamalı CI/CD pipeline'ının oluşturulması (.github/workflows/ci.yml).
  * dotnet format --verify-no-changes ile otomatik Clean Code ve stil kapısı (Style Gate).
  * SonarQube / SonarCloud entegrasyonu, XPlat Code Coverage (Cobertura/OpenCover) raporlaması ve otomatik artifact yükleme.
  * Catalog.Api ve ApiGateway için Docker container build doğrulama (Container Integrity Gate).
* [x] Task 1.7: SonarQube Kalite Kapısı, Kod Kapsama (Code Coverage) ve Güvenlik Sıkılaştırması
  * Dockerfile'lar üzerinde Least Privilege prensibi ile non-root `USER app` güvenliği (S6471).
  * CORS mimarisinin güvenli olarak incelenmesi ve onaylanması (S5122).
  * appsettings dosyalarından sabit Vault adreslerinin arındırılması ve 12-factor env yönetimi.
  * BaseEntity eşitlik operasyonlarının Microsoft standartlarına (S3875) uyarlanması.
  * Domain event snapshotting ve Outbox bütünlüğünün `.ToList()` ile güvenceye alınması.
  * Kod kapsama oranının (Code Coverage) %90.2'ye çıkarılması, 171 testin hatasız geçmesi ve SonarQube Quality Gate'in (Green / OK) tescillenmesi.
* [x] Task 1.8: Mimari Bağımlılık ve Testcontainers Tabanlı Gerçek Entegrasyon Test Altyapısı
  * NetArchTest.Rules ile Onion Architecture katman bağımlılıklarını (Domain, Application, Infrastructure, Api) ve CQRS/DDD tasarım kurallarını denetleyen merkezi Architecture Tests projesinin kurulması.
  * Testcontainers for .NET (PostgreSQL) entegrasyonu ile InMemory yerine gerçek Docker veritabanı container'ı üzerinde çalışan izole entegrasyon test altyapısının kurulması.
  * WebApplicationFactory (ASP.NET Core Mvc Testing) ile Minimal API endpoint'lerinin gerçek HTTP istekleriyle uçtan uca test edilmesi.
* [x] Task 1.9: Temel Gözlemlenebilirlik (Observability), Asenkron Serilog Sinks Mimarisi, Outbox Zehirli Mesaj Direnci, Vault Seeding ve Idempotency Temeli
  * CorrelationId & Context Propagation: İstemciden gelen `X-Correlation-ID` header'ını yakalayan, yoksa üreten, Serilog LogContext ve W3C Activity etiketlerine enjekte eden, downstream HTTP çağrılarına taşıyan `CorrelationIdMiddleware` ve `CorrelationIdDelegatingHandler`.
  * Asenkron ve Genişletilebilir Serilog Mimarisi: `Serilog.Sinks.Async` ile non-blocking ring buffer tabanlı; konfigürasyon (`appsettings`/Vault) ve kod üzerinden Console, File (Rolling File), PostgreSQL sink'lerini destekleyen ve yeni sink'lerin (Seq, Loki, Elastic) `ILogSinkConfigurator` ile kolayca takılabildiği modüler loglama altyapısı.
  * OpenTelemetry Enstrümantasyonu: ASP.NET Core, HttpClient ve EF Core çağrılarının W3C trace standartlarında izlenmesi ve OTLP exporter hazırlığı.
  * Standart Health Checks: Liveness (`/health/live`) ve Readiness (`/health/ready`) probe'larının standart JSON sözleşmesiyle sunulması.
  * MediatR Telemetri & PII Maskeleme: `LoggingBehavior` (hassas verileri - şifre, token, kart no - `[MaskSensitiveData]` ve anahtar kelime eşleşmesiyle maskeleyen PII Masker) ve `PerformanceBehavior` (500 ms üzeri yavaş istekleri uyaran timer).
  * Outbox Dayanıklılığı ve Zehirli Mesaj Koruması: `OutboxMessage` entity'sinde `RetryCount`, PostgreSQL kısmi indeksi (`WHERE "ProcessedOnUtc" IS NULL`), `OccurredOnUtc` indeksi ve `MaxRetries = 3` ile head-of-line blocking önleme.
  * HashiCorp Vault Seeding & Sırsız AppSettings: Tüm loglama tercihleri, OpenTelemetry ayarları ve altyapı sırlarının `init-vault.sh` seed scriptine taşınması, `appsettings.json` dosyalarının sır ve operasyonel değerlerden arındırılması.
  * Inbox & Integration Event Sözleşmeleri: `IIntegrationEvent` ve `InboxMessage` entity'leri ile idempotent event tüketimi temeli.

# Milestone 2: API Gateway ve Identity (Auth) Service
Kullanıcı girişlerinin, sosyal kimlik doğrulamanın ve sistem trafiğinin yönetilmesi.
* [x] Task 2.1: YARP API Gateway Kurulumu
  * YARP tabanlı Gateway projesinin oluşturulması.
  * Mobil uygulamalardan gelecek yüksek istekleri (özellikle auth ve shuffle endpoint'lerini) korumak için Rate Limiting konfigürasyonlarının yapılması.
  * Gateway seviyesinde JWT Validasyonu (Authentication) yapısının kurulması.
  * Role & Claims bazlı Yetkilendirme (Authorization) politikalarının YARP route'larına entegre edilmesi (Böylece geçersiz token'a veya yetkisiz claim'e sahip istekler iç servislere hiç ulaşmadan Gateway'den döner).
* [x] Task 2.2: Identity Service Geliştirmesi
  * POST /api/auth/register: Yeni kullanıcı e-posta/şifre kaydı.
  * POST /api/auth/login: Credential doğrulaması ve JWT + Refresh Token dönülmesi.
  * POST /api/auth/refresh: Süresi dolan JWT'nin refresh token ile yenilenmesi.
  * POST /api/auth/revoke: Kullanıcı çıkış yaptığında token'ın geçersiz kılınması.
  * POST /api/auth/guest: Misafir kullanıcı oturumu oluşturma (Anonim session ID, Guest JWT ve başlangıç 3 bilet hakkı ile sıfır sürtünmeli başlatma).
  * POST /api/auth/social-login: Apple ve Google SSO (Tek Tıkla Giriş) ID token doğrulaması ve otomatik hesap eşleme.
  * POST /api/auth/merge-guest: Misafir oturumundaki verilerin (beğenilen/kaydedilen içerikler, kalan biletler ve swipe geçmişi) yeni oluşturulan veya giriş yapılan kalıcı hesaba aktarılması (Data Merge).
  * DELETE /api/auth/account: Kullanıcı hesabını ve tüm kişisel verilerini kalıcı olarak silme (Apple App Store Guideline 5.1.1(v) ve GDPR/KVKK yasal uyumluluk gereksinimi).
* [x] Task 2.3: Güvenlik ve Token Blacklist Mimarisi
  * Sektörde kullanılan tüm JWT güvenlik mekanizmalarının oluşturulması (`jti`, `iat`, `nbf`, `security_stamp`, katı `ClockSkew = TimeSpan.Zero` tolerans sıfırlaması - OWASP API4).
  * Revoke edilen (kullanımdan kalkan) token'lar için Redis üzerinde yüksek performanslı iki kademeli Blacklist (`ITokenBlacklistService` & `RedisTokenBlacklistService`):
    * Tekil token seviyesinde kalan TTL ömrüyle `blacklist:token:{jti}` kaydı.
    * Hesap silme (`/account`) ve Token Reuse saldırılarında tüm oturumları anında geçersiz kılan `blacklist:user:{userId}` Unix zaman damgası kaydı.
  * API Gateway seviyesinde `OnTokenValidated` hook'u ile Redis Blacklist sınır denetimi (Edge Enforcement) sağlanarak iptal edilmiş token'ların downstream mikroservislere ulaşmadan RFC 7807 `401 Unauthorized` (`AUTH_TOKEN_REVOKED`) ile kesilmesi.
  * HashiCorp Vault üzerinden merkezi Redis yapılandırması (`Redis:Host`, `Redis:Port`, `Redis:Password`), fail-open dayanıklılık stratejisi ve 35/35 tam başarılı uçtan uca `curl` test paketi ile doğrulanması.
* [ ] Task 2.4: Outbox Pattern ve Identity Event Choreography
  * Dağıtık sistem tutarlılığı için Transactional Outbox Pattern uygulanması.
  * UserRegisteredEvent fırlatılması (Tüketenler: Notification Service -> Hoş geldin e-postası, Ticket Economy -> 14 günlük balayı bilet kotası ve streak/XP profilini ilklendirme, Profile Service -> Başlangıç profil kaydı).
  * UserMergedEvent fırlatılması (Tüketenler: User Library -> Misafir watchlist kayıtlarını hesaba aktarma, Ticket Economy -> Kalan biletleri aktarma, History & Analytics -> Telemetriyi bağlama).
  * UserAccountDeletedEvent fırlatılması (Tüketenler: Profile, User Library, Ticket Economy, Notification -> Cihaz tokenlarını ve kişisel verileri temizleme).

# Milestone 3: Catalog Service (The Source of Truth)
İçeriklerin (film, dizi, antoloji bölümleri), yayın platformlarının ve özgün ruh hali türlerinin CQRS ve Event-Driven prensiplerle inşası.
* [ ] Task 3.1: Catalog CRUD ve Detay İşlemleri (Read & Write)
  * POST / PUT / DELETE /api/catalog/movies: Film yönetimi.
  * GET /api/catalog/movies/{id}: Film detay sayfası (özet, afiş, süre, IMDb puanı, yönetmen/oyuncu kadrosu, platform deep link'leri ve his etiketleri).
  * POST / PUT / DELETE /api/catalog/series: Dizi yönetimi.
  * GET /api/catalog/series/{id}: Dizi detay sayfası, sezon ve bölüm hiyerarşisi.
  * POST / PUT / DELETE /api/catalog/series/{seriesId}/episodes: Diziye bölüm ekleme/çıkarma.
  * GET /api/catalog/episodes/{id}: Bağımsız ve antoloji bölüm detay sayfası (Bölüm Ruleti kart detayları için).
  * Antoloji serileri ("Black Mirror", "Love Death & Robots" vb.) için dizi ve bölüm seviyesinde IsStandalone flag'i, süre (runtime ~45 dk) ve bölüm thumbnail/afiş desteği.
  * Dijital Yayın Platformları Desteği:
    * GET /api/catalog/platforms: Desteklenen dijital yayın platformları (Netflix, Prime Video, Disney+ vb. ID, ad, logo URL, temel şema URL).
    * POST / PUT / DELETE /api/catalog/platforms: Platform tanımlama ve yönetimi.
    * Medya içeriklerine platform atama ve "Platformda Aç / Hemen İzle" deep link URL'lerinin (appSchemeUrl / webUrl) kaydedilmesi.
  * Özgün Ruh Hali / Mood Türleri Desteği (FRD Kısıtlamaları):
    * GET /api/catalog/moods: Özgün ruh hali listesi ("Kafa Dağıtmalık", "Beyin Yakanlar", "Gerim Gerim Gerenler", "Gözyaşı Garantili", "Dünyadan Uzaklaş", "Gaza Getirenler"), alt açıklamaları, vibe etiketleri ve ambient blur renk kodları.
    * POST / PUT / DELETE /api/catalog/moods: Mood tanımlama ve yönetimi.
    * POST / PUT /api/catalog/media/{id}/moods: İçeriklerin bu özgün ruh hali türleriyle ilişkilendirilmesi.
  * Mood Türleri ve Platform dataları yvaş değişen değerler olduğu için cache'leme mekanizması get servisi için kullanılmalı.
  * Silme işlemlerinde Milestone 1'de kurulan Soft Delete altyapısının kullanılması.
* [ ] Task 3.2: Outbox Pattern ve RabbitMQ Entegrasyonu
  * Dağıtık sistem tutarsızlığını önlemek için veritabanı yazma işlemi ile event fırlatma işlemini tek transaction'da garantiye alacak Outbox Pattern'in kurulması.
  * Veri eklendiğinde/güncellendiğinde/silindiğinde MediaCreatedEvent, MediaUpdatedEvent, MediaDeletedEvent, PlatformCreated/UpdatedEvent ve MoodCreated/UpdatedEvent mesajlarının RabbitMQ'ya atılması.

# Milestone 4: Shuffle Engine Service (The Core Engine)
Sistemin en çok trafik alacak ana damarının performans, esneklik ve oyunlaştırma odaklı geliştirilmesi.
* [ ] Task 4.1: Event-Driven Veri Senkronizasyonu ve Redis In-Memory Altyapısı
  * RabbitMQ üzerinden fırlatılan medya, platform ve mood event'lerinin dinlenerek Redis Sorted Sets, Hash ve Set yapılarının anlık güncellenmesi.
  * InBox Pattern kullanılarak aynı event'in iki kez işlenmesinin (Idempotency) önüne geçilmesi.
  * UserSubscribedEvent ve UserSubscriptionCancelledEvent mesajlarının dinlenerek kullanıcının Redis üzerindeki Premium statüsünün ve yetkilerinin milisaniyede güncellenmesi.
  * Redis'in boş olması (Cold Start) durumunda, Shuffle Engine'in gRPC üzerinden Catalog servisine bağlanarak başlangıç verisini (Bulk) çekmesi ve Redis'i doldurması (Cache Warming).
* [ ] Task 4.2: Shuffle Endpoint'leri ve Swipe Arenası Entegrasyonu
  * GET /api/shuffle/movies?moodId={moodId}&platformIds={platformIds}&year={year}&minRating={minRating}: Film Gecesi modu için rastgele film getirme.
  * GET /api/shuffle/series?moodId={moodId}&platformIds={platformIds}&minRating={minRating}: Dizi Keşfet modu için rastgele dizi getirme.
  * GET /api/shuffle/episodes/roulette?moodId={moodId}&platformIds={platformIds}&minRating={minRating}: Bölüm Ruleti modu (Tek bir diziye bağlı kalmaksızın tüm antoloji dizilerinin bağımsız bölümlerinden oluşan ortak havuzdan 45 dakikalık çerezlik rastgele bölüm getirme).
  * GET /api/shuffle/series/{seriesId}/episodes: Arama kartından veya dizi detayından tetiklenen "Sadece Bu Diziyi Shuffle Yap" modu (Örn: Yalnızca Black Mirror bölümleri arasında rulet).
  * GET /api/shuffle/similar/{mediaId}: Detay sayfası ve Akıllı Köprü (Smart Bridge) için benzer ruh halindeki (mood) yapımlardan rulet formatında öneri getirme (Örn: Inception benzeri "Beyin Yakanlar" ruleti veya kullanıcının platformunda olmayan içeriğin benzerleri).
  * GET /api/shuffle/vip-weekend: "Hafta Sonu Garantili Başyapıtlar" VIP arenası (Yalnızca Premium kullanıcılara özel küratör onaylı başyapıt havuzundan rulet çevirme).
  * POST /api/shuffle/my-list?type={movie|series|episode}: "Listemden Shuffle Yap" (Yalnızca kullanıcının "İzleyeceklerim" listesindeki içeriklerden rulet çevirme; Premium yetki kontrolü zorunludur).
  * POST /api/shuffle/swipe-action: Swipe Arenası aksiyon yönetimi:
    * Sola kaydırma (Pas Geç): Bilet bakiyesinden 1 adet düşer ve kesintisiz sıradaki öneri kartını döner (Bilet sıfırsa kıtlık kancası yanıtı üretir).
    * Sağa kaydırma (Listeme Ekle): Bilet düşürmez; kütüphaneye ekleme tetikler ve ekranda platforma giden "Hemen İzle" butonunu aktif eder.
    * Her swipe eyleminde asenkron olarak ShuffleSwipedEvent fırlatılması (Tüketenler: Ticket Economy -> Bilet düşümü doğrulaması, History & Analytics -> Arka plan telemetri kaydı).
  * Çark çevrilmeden önce kullanıcının bilet bakiyesinin (veya Premium sınırsızlık durumunun) doğrulanması.
* [ ] Task 4.3: Performans (Heap Allocation Minimizasyonu)
  * Performanslı array ve bellek manipülasyonları için C#'ın Span<T>, Memory<T> ve ArrayPool<T> yapılarının kullanılması (GC yükünün minimize edilmesi).
  * Cache Stampede (Yığılma) problemine karşı Distributed Locking veya Background Cache Warming stratejilerinin uygulanması.
* [ ] Task 4.4: Polly ile Fallback (Circuit Breaker)
  * Redis çökerse veya yanıt veremezse, Polly kullanılarak sistemin geçici olarak "Günün Popülerleri" (statik liste) dönmesini sağlayacak Fallback ve Circuit Breaker mekanizmasının kurulması.

# Milestone 5: Search Service
Elasticsearch kullanarak hızlı ve Event-Driven metin arama sisteminin kurulması.
* [ ] Task 5.1: Elasticsearch Entegrasyonu ve Arama/Keşif Endpoint'leri
  * GET /api/search?q={text}&type={all|series|movies|episodes}&platformId={platformId}&moodId={moodId}&page=&pageSize=: Canlı arama ve filtreleme sonuçları (Debounce desteği, 16:9 yatay kart formatı, kullanıcının platformunda bulunma durumunu bildiren isAvailableOnUserPlatforms alanı ve Akıllı Köprü yönlendirme desteği).
  * GET /api/search/suggest?q={text}: Otomatik tamamlama (Edge N-Gram tokenizers kullanımı).
  * GET /api/search/trending-roulettes: Günün Popüler Ruletleri (Boş arama ekranında diğer kullanıcıların o gün en çok listesine eklediği 3-4 trend yapım mini kartları).
  * GET /api/search/recent: Kullanıcının son arama geçmişi (Arama ekranı boş durumundaki silinebilir çipler; yapılan aramalar otomatik olarak bu listeye işlenir).
  * DELETE /api/search/recent/{term}: Belirli bir arama teriminin geçmişten silinmesi.
  * DELETE /api/search/recent: Kullanıcının tüm arama geçmişini temizlemesi.
* [ ] Task 5.2: Eventual Consistency ve InBox Pattern
  * RabbitMQ Consumer'larının InBox Pattern (Idempotency) ile kurgulanması:
    * MediaCreatedEvent, MediaUpdatedEvent ve MediaDeletedEvent dinlenerek Elasticsearch doküman indeksinin güncellenmesi (Nihai Tutarlılık).
    * PlatformUpdatedEvent ve MoodUpdatedEvent dinlenerek indeksteki denormalize platform/ruh hali metadata alanlarının yenilenmesi.
    * MediaAddedToWatchlistEvent dinlenerek "Günün Popüler Ruletleri" (Trending) sayaçlarının ve arama popülerlik skorlarının güncellenmesi.

# Milestone 6: Profile & Preferences Service
Kullanıcının uygulamadaki "kişiliğini" (platformları, his profili ve bildirim ayarları) yönetecek esnek yapının kurulması.
* [ ] Task 6.1: Dinamik Veri Yönetimi
  * JSONB destekli PostgreSQL veritabanının kurgulanması.
* [ ] Task 6.2: Kullanıcı Tercihleri Endpoint'leri
  * GET /api/profile/me: Profil detayları (Avatar, ünvan, genel ayarlar).
  * PUT /api/profile/me: Profil bilgilerini güncelleme.
  * GET / PUT /api/profile/preferences/platforms: Kullanıcının abone olduğu yayın platformlarının yönetimi ("Platformlarım" ekranı).
  * GET / PUT /api/profile/preferences/moods: Kullanıcının favori ruh hali profili ("Tür Profilim" ekranı).
  * GET / PUT /api/profile/preferences/notifications: Bildirim tercihleri (Favori tür bildirimleri, Seri hatırlatıcı 🔥, Hafta sonu sürprizi açma/kapatma).
* [ ] Task 6.3: Shuffle Engine İletişimi (gRPC / Redis) ve Event Choreography
  * Shuffle Engine'in tercihleri milisaniyeler içinde okuyabilmesi için gRPC entegrasyonu veya ortak Redis kümesi üzerinden (Asenkron) tercih senkronizasyonunun yapılması.
  * Outbox Pattern: Profil ve tercihler değiştiğinde RabbitMQ'ya ProfileUpdatedEvent atılarak Shuffle Engine'deki pre-computed önbelleğin temizlenmesi (Cache Invalidation).
  * InBox Consumer'ları:
    * UserRegisteredEvent dinlenerek başlangıç profil kaydı ve varsayılan bildirim ayarlarının oluşturulması.
    * BadgeUnlockedEvent dinlenerek profil ekranındaki aktif ünvanın (örn: "Antoloji Avcısı") otomatik güncellenmesi.
    * UserAccountDeletedEvent dinlenerek kullanıcı profilinin kalıcı silinmesi/anonimleştirilmesi.

# Milestone 7: User Library ("Listem") Service
Kullanıcıların "İzleyeceklerim" ve "İzlediklerim" arşivlerini, alt kategori filtrelerini ve 3 durumlu oylama mekanizmasını yöneten bağımsız Bounded Context.
* [ ] Task 7.1: İzleyeceklerim (Watchlist) ve İzlediklerim Yönetimi
  * GET /api/library/watchlist?type={all|movies|series|episodes}&page=&pageSize=: "İzleyeceklerim" sekmesi (16:9 yatay afiş kartı, platform logosu, eklenme sebebi his etiketi, sayfalı liste).
  * POST /api/library/watchlist: Listeye içerik ekleme (Swipe Arenası sağa kaydırma eyleminden veya detay sayfasından tetiklenir).
  * DELETE /api/library/watchlist/{mediaId}: Listeden içerik silme (Liste içi sola kaydırma eylemi).
  * POST /api/library/watched: İçeriği "İzlediklerim" sekmesine taşıma (Liste içi sağa kaydırma eylemi) veya doğrudan izlendi olarak kaydetme.
  * DELETE /api/library/watched/{mediaId}: İçeriği "İzlediklerim" sekmesinden çıkarma / silme.
  * GET /api/library/watched?type={all|movies|series|episodes}&page=&pageSize=: "İzlediklerim" sekmesi listesi.
* [ ] Task 7.2: 3 Durumlu Oylama Sistemi ve Event-Driven Entegrasyon
  * POST /api/library/watched/{mediaId}/rate: "İzlediklerim" sekmesindeki içeriği oylama ("Çift Başparmak / Beğendim / Beğenmedim" formatında 3 durumlu puanlama).
  * Outbox Pattern ile Event Fırlatma:
    * MediaAddedToWatchlistEvent: İçerik listeye eklendiğinde fırlatılır (Tüketenler: Search Service -> Günün Popüler Ruletleri sayaç artırımı, History & Analytics -> Kaydetme telemetrisi).
    * MediaWatchedEvent: İçerik izlendiğinde fırlatılır (Tüketenler: History & Analytics -> İzleme geçmişi zaman çizgisi).
    * MediaRatedEvent: İçerik puanlandığında fırlatılır (Tüketenler: Ticket Economy -> Her 3 oylamada +3 bilet tanımlama, History & Analytics -> Öneri modeli besleme).
  * InBox Consumer'ları:
    * UserMergedEvent: Misafir kullanıcının geçici watchlist kayıtlarının yeni oluşturulan hesaba aktarılması (Data Merge).
    * UserAccountDeletedEvent: Kullanıcı hesabını sildiğinde kütüphane verilerinin kalıcı olarak temizlenmesi.
  * Shuffle Engine için gRPC Uç Noktası: "Listemden Shuffle" özelliği için kullanıcının watchlist ID listesini ultra-hızlı dönen gRPC metodunun sunulması.

# Milestone 8: Ticket Economy, Gamification & Premium Subscription Service
Kullanıcı tutundurma (retention), bilet ekonomisi (Boiling Frog), oyunlaştırma (Streak, XP, Rozetler) ve mağaza içi satın alma (IAP) yönetimi.
* [ ] Task 8.1: Bilet Ekonomisi (Boiling Frog Stratejisi)
  * GET /api/tickets/balance: Bilet bakiyesi, sınırsızlık durumu (♾️), günlük kalan bilet ve UTC gece yarısı sıfırlanma sayacı.
  * POST /api/tickets/claim-ad-reward: Ödüllü reklam izleme tamamlandığında bilet tanımlama (+1 Bilet Kıtlık Kancası).
  * InBox Consumer'ları (Otomatik Bilet Düşümü ve Ödüllendirme):
    * UserRegisteredEvent: Yeni kullanıcıya 14 günlük balayı sınırsız bilet hakkı tanımlanması.
    * ShuffleSwipedEvent: Sola kaydırma (Pas) eyleminde bilet bakiyesinden 1 adet düşülmesi.
    * MediaRatedEvent: Her 3 içerik puanlandığında kullanıcıya otomatik "+3 Bilet Ödülü" tanımlanması.
    * UserMergedEvent: Misafir oturumundaki biletlerin kalıcı hesaba aktarılması.
    * UserAccountDeletedEvent: Kullanıcıya ait bilet verilerinin silinmesi.
  * Outbox: Bilet tükendiğinde TicketBalanceExhaustedEvent fırlatılması (Kıtlık kancası tetikleyici).
  * Arka Plan İşi (Worker): 14 günlük balayı dönemi takibi, sonrasında günlük 15 ücretsiz bilet tanımlama ve her gece yarısı kota yenilemesi.
* [ ] Task 8.2: Oyunlaştırma (Gamification) - Seri (🔥), XP ve Rozetler
  * GET /api/gamification/me: Günlük Seri Durumu (🔥 Streak count), XP seviyesi ve ilerleme çubuğu, kazanılmış en yüksek ünvan (örn: "Antoloji Avcısı"), kazanılmış ve kilitli 3D rozet vitrini (örn: "Sınırsız Sinefil").
  * GET /api/gamification/badges: Tüm 3D rozetler kataloğu (Kazanılma koşulları, kilit durumu, rozet ikonları ve Instagram Story paylaşım metadataları).
  * POST /api/gamification/claim-streak: Uygulamaya 7 gün üst üste giriş yapan kullanıcılar için "Sadakat Bileti" ödülünün talep edilmesi.
  * Outbox: Seviye atlandığında LevelUpEvent, rozet kazanıldığında BadgeUnlockedEvent fırlatılması (Tüketenler: Notification Service -> Tebrik bildirimi, Profile Service -> Ünvan güncelleme).
  * InBox Consumer'ları:
    * UserRegisteredEvent: Yeni kullanıcı için Streak=1 ve Level=1 başlangıç gamification kaydının açılması.
    * UserAccountDeletedEvent: Rozet ve XP verilerinin silinmesi.
  * Günlük giriş ve swipe aksiyonlarında serinin artırılması, 24 saat işlem yapılmadığında serinin sıfırlanması kurgusu.
* [ ] Task 8.3: Premium Abonelik & In-App Purchase (IAP) Mimarisi
  * GET /api/subscriptions/plans: Dinamik abonelik paketleri ve paywall konfigürasyonu (Apple/Google Store Product ID'leri, fiyatlandırma, indirim etiketleri ve avantaj listesi).
  * POST /api/subscriptions/apple/verify: Apple App Store (StoreKit 2) JWS makbuz doğrulaması.
  * POST /api/subscriptions/google/verify: Google Play Billing purchase token doğrulaması.
  * GET /api/subscriptions/status: Kullanıcı aktif abonelik durumu, geçerlilik tarihi, paket tipi (Aylık/Yıllık/Sınırsız).
  * Server-to-Server Webhook'lar: POST /api/subscriptions/webhooks/apple ve /google (Yenileme, iptal, geri ödeme durumlarının anlık işlenmesi).
  * Outbox & Event Choreography (Premium Rol Yükseltme ve Düşürme):
    * UserSubscribedEvent: Abonelik başladığında veya yenilendiğinde fırlatılır (Tüketenler: Identity Service -> Kullanıcının 'Standard' veya 'Guest' rolünü 'Premium'a yükseltme (UserRole güncelleme ve VIP claim atama), Shuffle Engine -> Redis cache sınırsız bilet ve VIP arena kilidi açma, Notification Service -> Tebrik push/email, History & Analytics -> Dönüşüm analitiği).
    * UserSubscriptionCancelledEvent: Abonelik süresi dolduğunda, iptal veya iade edildiğinde fırlatılır (Tüketenler: Identity Service -> 'Premium' rolünü iptal edip 'Standard'a düşürme (Role Downgrade), Shuffle Engine -> Redis cache yetkilerini standart kotalara çekme, History & Analytics -> Churn analitiği).
  * Kullanıcı Premium Geçiş Modelleri:
    * 1. In-App Yükseltme: Mevcut Standart veya Guest kullanıcının mobil paywall üzerinden StoreKit 2 / Google Play ile abone olup makbuz doğrulaması sonucu asenkron rol yükseltmesi.
    * 2. Doğrudan Premium Hesap Açma: Web checkout veya promosyon kodu ile kayıt akışında ödeme onayı sonrası hesabın otomatik Premium statüsüne evrilmesi.

# Milestone 9: History, Analytics & Notification Servisleri
Kullanıcı telemetrisi ve mobil cihazlarla etkileşim/tutundurma bildirimleri.
* [ ] Task 9.1: History & Analytics Service (MongoDB)
  * POST /api/analytics/events/batch: Mobil istemciden pil ve ağ tasarrufu amacıyla swipe, kartta kalma süresi (dwell time) ve oturum verilerini toplu (batch ingestion) olarak kabul eden yüksek throughput'lu asenkron endpoint (202 Accepted).
  * POST /api/analytics/click-out: Kullanıcı kart üzerindeki "Hemen İzle / Platformda Aç" butonuna basıp dijital yayın platformuna (Netflix, Prime Video vb.) yönlendirildiğinde bu dönüşümün (outbound conversion) kaydedilmesi.
  * GET /api/history/users/me: Kullanıcının kronolojik izleme ve etkileşim geçmişi sorgulama.
  * GET /api/analytics/users/me/dashboard: Profil ekranındaki özet istatistikleri (toplam keşif sayısı, pas geçme oranı, en çok tüketilen ruh hali türleri ve tahmini kazanılan vakit) besleyen MongoDB aggregation endpoint'i.
  * GET /api/analytics/users/me/monthly-habits: Profil dashboard'unda yer alan "Aylık İzleme Alışkanlıkları" grafiğini besleyen zaman serisi analitik verisi.
  * GET /api/analytics/trends/moods: Sistem genelinde en çok çark çevrilen ve tutulan ruh hallerinin trend analitiği (Shuffle Engine Cache Warming ve Fallback popüler listelerini beslemek için).
  * InBox Consumer'ları (Asenkron Telemetri Alımı):
    * ShuffleSwipedEvent: Sola/sağa kaydırma telemetrisi ve kartta kalma süresinin kaydedilmesi.
    * MediaAddedToWatchlistEvent ve MediaWatchedEvent: Kullanıcı dönüşüm ve izleme geçmişi zaman çizelgesinin oluşturulması.
    * MediaRatedEvent: 3 durumlu puanlama verisinin yapay zeka öneri modelini beslemek üzere işlenmesi.
    * UserMergedEvent: Misafir oturumundaki telemetri verilerinin kalıcı hesap ile eşleştirilmesi.
    * UserSubscribedEvent: Premium dönüşüm metriklerinin analitik veri tabanına işlenmesi.
    * UserAccountDeletedEvent: Kullanıcı telemetrisinin KVKK/GDPR "unutulma hakkı" uyarınca anonimleştirilmesi.
  * MongoDB'de telemetri ve analitik verilerinin UserId'ye (Shard Key) göre Sharding kurgusu ile yatayda ölçeklenebilir dağıtılması.
* [ ] Task 9.2: Notification (Push & Email) Service Core
  * POST /api/notifications/devices: Firebase Device Token (iOS/Android APNs & FCM) kaydı.
  * GET /api/notifications/me & PUT /api/notifications/{id}/read: Uygulama içi bildirim zili/geçmişi.
  * PUT /api/notifications/read-all: Tüm okunmamış bildirimleri tek tıkla okundu olarak işaretleme.
  * E-posta gönderim altyapısının (SMTP, SendGrid, Mailgun vb.) Notification servisine entegre edilmesi.
* [ ] Task 9.3: Event-Driven Push & Retention Bildirim Akışları
  * InBox Consumer'ları (Event-Driven Bildirimler):
    * UserRegisteredEvent dinlenerek yeni kayıt olan kullanıcıya "Hoş Geldin" e-postasının asenkron gönderilmesi.
    * MediaCreatedEvent dinlenerek gRPC üzerinden Profile servisinden ilgili türü seven ve bildirimi açık kullanıcılara Push atılması.
    * BadgeUnlockedEvent ve LevelUpEvent dinlenerek kullanıcıya anlık "Tebrikler, yeni bir rozet kazandın! 🏆" kutlama push bildirimi atılması.
    * UserSubscribedEvent dinlenerek Premium üyelik onay e-postası ve VIP ayrıcalıklar bildiriminin iletilmesi.
    * UserAccountDeletedEvent dinlenerek kullanıcıya ait tüm FCM/APNs cihaz token'larının silinmesi.
  * Zamanlanmış Tutundurma (Retention) Bildirim İşleri (Worker):
    * Cuma akşamları sürpriz bilet push bildirimleri ("Cuma Sürprizi: Biletlerin Yüklendi! 🎟️").
    * Her akşam saat 20:00'de serisi bozulma tehlikesinde olan kullanıcılara "Serini Koru! 🔥" push bildirimi.
    * Premium kullanıcılara özel "Hafta Sonu Garantili Başyapıtlar" VIP arena tanıtım push bildirimleri.
    * Bildirim gönderiminde kullanıcının Profile.Preferences.Notifications ayarlarının filtrelenmesi.
  * Idempotency ve Resilience: Gönderilen bildirim hash'inin Redis'te saklanması, FCM/APNs çökmelerine karşı Polly Exponential Backoff ve DLQ (Dead Letter Queue) mimarisi.

# Milestone 10: Merkezi Gözlemlenebilirlik (Observability), APM ve Görselleştirme
Temelde atılan enstrümantasyon (Task 1.9) verilerinin merkezi dashboard'lar ve alarmlar ile operasyonel hale getirilmesi.
* [ ] Task 10.1: Prometheus & Grafana Dashboard'ları (Metrikler)
  * Prometheus üzerinden mikroservislerin `/metrics` endpoint'lerinin pull edilmesi ve scrape konfigürasyonlarının yapılması.
  * Grafana üzerinde SLO/SLA, latency (p95, p99), HTTP 5xx hata oranları, bellek/CPU tüketimi ve business metriklerini gösteren dashboard'ların tasarlanması.
  * Kritik eşik aşımlarında (hata oranı > %1, p99 > 1s) Slack / E-posta uyarı mekanizmasının (AlertManager) kurulması.
* [ ] Task 10.2: Jaeger / Tempo Dağıtık İzleme Paneli (Distributed Tracing UI)
  * OTLP ile toplanan trace verilerinin Jaeger / Grafana Tempo üzerinde görselleştirilmesi.
  * YARP Gateway -> Identity -> Catalog -> RabbitMQ -> Shuffle Engine uçtan uca istek akışının gecikme analizi (Waterfall trace view).
* [ ] Task 10.3: Merkezi Log Sunucusu ve Log Retention (Seq / Grafana Loki)
  * Serilog ile üretilen yapılandırılmış logların Seq veya Grafana Loki merkezi log deposuna yönlendirilmesi.
  * Log retention (saklama süresi), indeksleme ve arşivleme politikalarının tanımlanması.