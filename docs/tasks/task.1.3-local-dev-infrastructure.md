# Task 1.3: Local Development Altyapısı (Docker Compose) - Eğitim ve Mimari Dokümantasyonu

Bu doküman, ShuffleSeries mikroservis ekosisteminde yerel geliştirme ortamının (Local Development Environment) container tabanlı olarak ayağa kaldırılması sürecini, alınan mimari kararları, uygulanan desenleri ve doğrulama yöntemlerini açıklar.

---

## 1. Ne Yaptık? (Genel Özet)

ShuffleSeries platformundaki mikroservislerin (Catalog, Identity, Shuffle Engine, Search, History & Analytics vb.) geliştirme ortamında bağımlı olduğu tüm backing altyapı servislerini tek bir [`docker-compose.yml`](../../docker-compose.yml) altında topladık:

1. **PostgreSQL 16 (`postgres:16-alpine`):**
   * İlişkisel veri, Catalog, Identity ve Transactional Outbox tablolarını barındırır.
   * `docker/postgres/init-databases.sh` script'i ile container ilk başlatıldığında `shuffleseries_db` ana veritabanının yanında `shuffleseries_identity` gibi çoklu mikroservis veritabanlarını idempotently oluşturacak altyapı kuruldu.
2. **MongoDB 7.0 (`mongo:7.0`):**
   * History & Analytics Bounded Context'i için append-only telemetri ve kullanıcı izleme geçmişini depolar.
   * Modern `mongosh` tabanlı healthcheck konfigürasyonu yapıldı.
3. **Redis 7.4 (`redis:7.4-alpine`):**
   * Hızlı veri önbellekleme (Caching), Shuffle Engine In-Memory Store, Token Blacklist ve Distributed Lock ihtiyaçlarını karşılar.
   * `--requirepass` ile yetkilendirme ve `--appendonly yes` (AOF) ile veri kalıcılığı sağlandı.
4. **Elasticsearch 8.15.0 (`elasticsearch:8.15.0`):**
   * Search Service için full-text search, bulanık arama ve autocomplete/edge n-gram işlemlerini yönetir.
   * Tek düğümlü (`discovery.type=single-node`) ve geliştirici makinelerinde RAM darboğazını önlemek için JVM bellek limiti (`ES_JAVA_OPTS=-Xms512m -Xmx512m`) ile sınırlandırıldı.
5. **RabbitMQ 3.13 (`rabbitmq:3.13-management-alpine`):**
   * Asenkron mikroservis haberleşmesi (Event-Driven Architecture) için AMQP broker (`:5672`) ve web tabanlı yönetim paneli (`:15672`).
6. **Güvenlik ve Yapılandırma Standartları:**
   * Hassas kimlik bilgilerini kaynak koddan ayıran ve yeni geliştiricilerin projeyi tek adımda klonlayıp başlatmasını sağlayan [`.env.example`](../../.env.example) şablonu oluşturuldu.
   * Kök dizindeki eski/eksik `compose.yaml` temizlendi.

---

## 2. Neden Yaptık? (Mimari Kararlar ve "Neden?" Analizi)

### Soru 1: Mikroservis Dockerfile'larını neden docker-compose.yml içine dahil etmedik?
* **Problem:** Bir geliştirici yerel ortamda Rider veya Visual Studio açıp kod yazarken, mikroservislerin her satır değişikliğinde Docker image'ı yeniden build etmesini beklemek geri bildirim döngüsünü (Inner Loop Development Speed) felce uğratır.
* **Karar:** `docker-compose.yml` sadece **Backing Services (Altyapı Servisleri)** için tasarlandı. Geliştiriciler altyapıyı `docker compose up -d` ile arka planda çalıştırır, kendi mikroservislerini ise IDE'lerinde debug modunda (hot reload, breakpoint, test runner) anında çalıştırır. Mikroservis Dockerfile'ları CI/CD ve her servisin kendi teslim aşamasında bağımsız olarak ele alınır.

### Soru 2: Neden her mikroservis için ayrı bir PostgreSQL container'ı yerine tek container + çoklu veritabanı (Multi-Database per Service) seçtik?
* **Problem:** Mikroservis mimarisinde "Database-per-Service" temel kuraldır. Ancak yerel ortamda her servis için ayrı bir PostgreSQL container'ı ayağa kaldırmak (Catalog için Postgres, Identity için Postgres vb.) RAM ve CPU israfına yol açar.
* **Karar:** Mantıksal izolasyon (Logical Isolation) ilkesini uyguladık. Tek bir PostgreSQL container'ı içerisinde `shuffleseries_db` (Catalog) ve `shuffleseries_identity` (Identity) olmak üzere tamamen izole veritabanları tanımlandı. `init-databases.sh` ile bu işlem sıfır el müdahalesiyle otomatikleştirildi.

### Soru 3: Elasticsearch için neden JVM bellek kısıtlaması (`-Xms512m -Xmx512m`) uyguladık?
* **Problem:** Elasticsearch varsayılan ayarlarda ana makinedeki boş RAM'in %50'sine kadarını rezerve edebilir. Bu da 16GB RAM'e sahip tipik geliştirici bilgisayarlarında Docker Desktop'un çökmesine veya işletim sisteminin swap'e düşmesine sebep olur.
* **Karar:** Yerel geliştirme veri setleri küçük olduğu için 512MB heap boyutu fazlasıyla yeterlidir.

### Soru 4: MongoDB healthcheck'inde neden `mongo` yerine `mongosh` kullandık?
* **Problem:** MongoDB 6.0 sürümünden itibaren eski `mongo` CLI binary'sini container imajından kaldırmıştır. Eski healthcheck script'leri (`mongo --eval ...`) sessizce hata verir veya container'ı sağlıksız (unhealthy) olarak işaretler.
* **Karar:** Resmi MongoDB kabuğu olan `mongosh` kullanıldı.

---

## 3. Hangi Pattern'leri ve Prensipleri Kullandık?

1. **Database-per-Service Pattern (Logical Isolation):**
   * Her mikroservisin veritabanı mantıksal olarak birbirinden izoledir. Catalog servisi Identity veritabanına, Identity servisi Catalog veritabanına erişemez.
2. **Infrastructure as Code (IaC):**
   * Tüm geliştirme ortamı bağımlılıkları bildirimsel (declarative) bir YAML sözdizimi ile versiyon kontrolüne alındı.
3. **Twelve-Factor App (Config & Backing Services):**
   * Config (III): Tüm port, şifre ve host ayarları koddan ayrı `.env` ortam değişkenleri üzerinden enjekte edilir.
   * Backing Services (IV): Veritabanları ve mesaj kuyrukları ekli kaynaklar (attached resources) olarak kabul edilir ve bağlantı dizeleri değiştirilerek anında yerel/bulut ayrımı yapılabilir.
4. **Health Check Pattern:**
   * Her servis için aktif durum denetimi (ping, query) tanımlandı. Böylece orkestrasyon araçları servisin sadece port açmasını değil, sorgu kabul edebilir durumda olduğunu garanti eder.

---

## 4. Yapılan Geliştirme Nasıl Test Edilir?

### Test Senaryosu 1: Docker Compose Doğrulama ve Başlatma
```bash
# 1. Konfigürasyonu doğrula
docker compose config

# 2. Servisleri arka planda başlat
docker compose up -d

# 3. Tüm servislerin 'healthy' olduğunu doğrula
docker compose ps
```
**Beklenen Sonuç:** 5 container'ın tamamı (`shuffleseries_postgres`, `shuffleseries_mongodb`, `shuffleseries_redis`, `shuffleseries_elasticsearch`, `shuffleseries_rabbitmq`) `Up (healthy)` durumunda görünmelidir.

### Test Senaryosu 2: PostgreSQL Çoklu Veritabanı Doğrulama
```bash
docker exec shuffleseries_postgres psql -U admin -d shuffleseries_db -c "\l"
```
**Beklenen Sonuç:** Listede hem `shuffleseries_db` hem de `shuffleseries_identity` veritabanları listelenmelidir.

### Test Senaryosu 3: MongoDB Bağlantı Testi
```bash
docker exec shuffleseries_mongodb mongosh -u admin -p SuperSecretMongoPassword2026!! --authenticationDatabase admin --eval "db.adminCommand('ping')"
```
**Beklenen Sonuç:** `{ ok: 1 }` yanıtı dönmelidir.

### Test Senaryosu 4: Redis Auth ve Ping Testi
```bash
docker exec shuffleseries_redis redis-cli -a SuperSecretRedisPassword2026!! ping
```
**Beklenen Sonuç:** `PONG` yanıtı dönmelidir.

### Test Senaryosu 5: Elasticsearch Cluster Sağlığı
```bash
curl -s http://localhost:9200/_cluster/health
```
**Beklenen Sonuç:** `"status":"green"` ve `"cluster_name":"shuffleseries-cluster"` içeren JSON dönmelidir.

### Test Senaryosu 6: RabbitMQ Management API & Web UI
* Web Tarayıcısı: `http://localhost:15672` (Kullanıcı: `guest_123`, Şifre: `guest_123`)
* CLI Testi:
```bash
curl -s -u guest_123:guest_123 http://localhost:15672/api/overview | grep -o '"rabbitmq_version":"[^"]*"'
```
**Beklenen Sonuç:** `"rabbitmq_version":"3.13.7"` bilgisi dönmeli ve Web UI açılmalıdır.

### Test Senaryosu 7: Catalog API Entegrasyonu & EF Core Migration
```bash
dotnet run --project ShuffleSeries.Catalog/ShuffleSeries.Catalog.Api
```
**Beklenen Sonuç:**
* Uygulama PostgreSQL'e bağlanıp migration'ları uygular.
* RabbitMQ'ya (`rabbitmq://localhost/`) MassTransit ile bağlanır.
* Quartz Outbox scheduler'ı başarıyla çalışmaya başlar.
