Bu doküman, ShuffleSeries projesinin uçtan uca geliştirme sürecini aşamalara (Milestones) ve görevlere (Tasks) bölerek detaylandırır. Proje; güvenli, ölçeklenebilir, yüksek performanslı ve "Senior" seviye mimari pratiklere (DDD, CQRS, Event-Driven, Observability) uygun olarak inşa edilecektir.

# Milestone 1: Altyapı, Çekirdek (Shared.Core) ve CI/CD Kurulumu
Mikroservislerin üzerinde yükseleceği ortak yapıların ve dağıtım kanallarının kurulması.
* [x] Task 1.1: Shared.Core Geliştirmeleri
  * Tüm servislerde kullanılacak BaseEntity, CustomException ve ortak DTO'ların oluşturulması.
  * Global Exception Handling Middleware ve standart ProblemDetails yanıt yapısının kurulması.
* [x] Task 1.2: Soft Delete Altyapısı
  * Entity'lere IsDeleted flag'i eklenerek EF Core seviyesinde Global Query Filter (Interceptor veya Base DbContext) ile Soft Delete mantığının entegre edilmesi.
* [ ] Task 1.3: Local Development Altyapısı (Docker Compose)
  * Geliştirme ortamında kullanılacak PostgreSQL, MongoDB, Redis, Elasticsearch ve RabbitMQ'nun yapılandırılarak tek bir docker-compose.yml dosyası ile ayağa kaldırılması. (Not: Mikroservis Dockerfile'ları burada değil, her servis geliştirildikçe kendi task'i içinde yazılacaktır).
* [ ] Task 1.4: Secret Management (Vault) Entegrasyonu
  * HashiCorp Vault altyapısının docker-compose ortamına servis olarak eklenmesi ve ayağa kaldırılması.
  * Shared.Core içerisine, mikroservisler ayağa kalkarken (Bootstrap aşamasında) konfigürasyonları doğrudan Vault üzerinden güvenli bir şekilde okuyacak .NET Configuration Provider entegrasyonunun yazılması.
  * Veritabanı connection string'leri, JWT Secret Key, RabbitMQ ve Redis şifreleri gibi tüm hassas verilerin kaynak koddan ve konfigürasyon dosyalarından arındırılarak tamamen Vault içerisine taşınması.
* [ ] Task 1.5: Swagger altyapısı kurulumu
* [ ] Task 1.6: CI/CD Pipeline (GitHub Actions)
  * Proje iskeleti için temel Build ve Test pipeline'ının oluşturulması.
  * SonarQube entegrasyonu ile kod kalite analizinin pipeline'a eklenmesi.

# Milestone 2: API Gateway ve Identity (Auth) Service
Kullanıcı girişlerinin, sosyal kimlik doğrulamanın ve sistem trafiğinin yönetilmesi.
* [ ] Task 2.1: YARP API Gateway Kurulumu
  * YARP tabanlı Gateway projesinin oluşturulması.
  * Mobil uygulamalardan gelecek yüksek istekleri (özellikle auth ve shuffle endpoint'lerini) korumak için Rate Limiting konfigürasyonlarının yapılması.
  * Gateway seviyesinde JWT Validasyonu (Authentication) yapısının kurulması.
  * Role & Claims bazlı Yetkilendirme (Authorization) politikalarının YARP route'larına entegre edilmesi (Böylece geçersiz token'a veya yetkisiz claim'e sahip istekler iç servislere hiç ulaşmadan Gateway'den döner).
* [ ] Task 2.2: Identity Service Geliştirmesi
  * POST /api/auth/register: Yeni kullanıcı e-posta/şifre kaydı.
  * POST /api/auth/login: Credential doğrulaması ve JWT + Refresh Token dönülmesi.
  * POST /api/auth/refresh: Süresi dolan JWT'nin refresh token ile yenilenmesi.
  * POST /api/auth/revoke: Kullanıcı çıkış yaptığında token'ın geçersiz kılınması.
  * POST /api/auth/guest: Misafir kullanıcı oturumu oluşturma (Anonim session ID, Guest JWT ve başlangıç 3 bilet hakkı ile sıfır sürtünmeli başlatma).
  * POST /api/auth/social-login: Apple ve Google SSO (Tek Tıkla Giriş) ID token doğrulaması ve otomatik hesap eşleme.
  * POST /api/auth/merge-guest: Misafir oturumundaki verilerin (beğenilen/kaydedilen içerikler, kalan biletler ve swipe geçmişi) yeni oluşturulan veya giriş yapılan kalıcı hesaba aktarılması (Data Merge).
  * DELETE /api/auth/account: Kullanıcı hesabını ve tüm kişisel verilerini kalıcı olarak silme (Apple App Store Guideline 5.1.1(v) ve GDPR/KVKK yasal uyumluluk gereksinimi).
* [ ] Task 2.3: Güvenlik ve Token Blacklist Mimarisi
  * Revoke edilen (kullanımdan kalkan) token'lar için Redis üzerinde bir Blacklist tutulması.
  * API Gateway JWT doğrularken bu Redis Blacklist'ini kontrol etmesinin (Security & Caching) sağlanması.
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
  * Outbox: Abonelik başladığında veya yenilendiğinde UserSubscribedEvent, iptal/iade durumunda UserSubscriptionCancelledEvent fırlatılması (Tüketenler: Shuffle Engine -> Redis cache yetki güncelleme, Notification Service -> Tebrik push/email, History & Analytics -> Dönüşüm analitiği).

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

# Milestone 10: Gözlemlenebilirlik (Observability) ve Monitoring
Sistemin "kör uçuş" yapmasını engelleyecek altyapıların entegrasyonu.
* [ ] Task 10.1: Metrics (Metrikler)
  * Servislere .NET Health Checks ve OpenTelemetry entegrasyonu.
  * /metrics endpoint'i açılarak CPU, RAM, HTTP 5xx hata oranları ve gecikmelerin Prometheus ile toplanması (pull).
  * Prometheus üzerinden pull edilen verilerle Grafana Dashboard'larının hazırlanması.
* [ ] Task 10.2: Distributed Tracing (Dağıtık İzleme)
  * OpenTelemetry ve Jaeger entegrasyonu ile mikroservisler arası isteklerin Trace ID ile uçtan uca (örn: Gateway -> Search -> Elasticsearch) takip edilmesi.
* [ ] Task 10.3: Centralized Logging (Merkezi Loglama)
  * Tüm servislerde logların konsola değil, yapılandırılmış (Structured JSON) log olarak Serilog aracılığıyla merkezi bir Seq veya Loki sistemine (TraceID ve UserId ile) aktarılması.
  * Request - Response loglaması API Gateway katmanında merkezi bir yapıda kişisel verilerin maskelenerek (PII Masking) loglanması.