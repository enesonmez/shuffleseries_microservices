# Task 1.7: SonarQube Kalite Kapısı, Kod Kapsama (Code Coverage) ve Güvenlik Sıkılaştırması

## 1. Ne Yaptık?
Bu görev kapsamında, ShuffleSeries mikroservis ekosisteminin statik kod analizi, güvenlik hotspot incelemeleri ve test kapsama oranları (Code Coverage) kurumsal üretim standartlarına (Production-Ready) ulaştırılmıştır:

1. **Security Hotspot İyileştirmeleri (S6471 & S5122):**
   - Hem `ShuffleSeries.Catalog.Api` hem de `ShuffleSeries.ApiGateway` Dockerfile'larında `USER app` direktifi eklenerek konteynerlerin root yetkisiyle çalışması engellendi (Least Privilege Principle).
   - `CorsExtensions.cs` üzerindeki CORS politikaları mimari olarak incelendi ve SonarQube arayüzünde güvenli (Safe) olarak işaretlendi.
2. **Sır ve Hassas Veri Yönetimi (API8 Hardcoded Secrets):**
   - `appsettings.json` ve `appsettings.Development.json` dosyalarından sabit Vault adresi kaldırılarak tamamen ortam değişkeni (`VAULT_ADDR` / `.env`) üzerinden beslenmesi sağlandı.
3. **BaseEntity Eşitlik (Equality) Refaktörü (S3875, S3897, S4035):**
   - Microsoft Framework Design Guidelines uyarınca, `operator ==` / `!=` aşırı yüklemeleri ve bağımsız `IEquatable<BaseEntity>` implementasyonu kaldırılarak tip güvenli `Equals(object?)` ve `GetHashCode()` yapısı benimsendi.
4. **Domain Event Temizleme & Deferred Execution Bug Fix:**
   - `InsertOutboxMessagesInterceptor` içerisinde aggregate domain event'lerinin `SelectMany` deferred execution sırasında listenin referans üzerinden temizlenmesiyle kaybolması sorunu tespit edildi. `.ToList()` snapshot kopyalama yöntemiyle event'lerin Outbox tablosuna eksiksiz aktarımı garanti altına alındı.
5. **Kapsamlı Test ve Test Kapsama Artışı (Coverage >= 90.2%):**
   - `coverage.runsettings` dosyası oluşturuldu; otomatik üretilen EF Core Migration'ları ve OpenAPI sınıfları kapsam dışı bırakıldı.
   - `CreateSeriesCommandValidatorTests`, `UpdateSeriesCommandValidatorTests`, `DeleteSeriesCommandValidatorTests`, `ValidationBehaviorTests`, `MediatorExtensionsTests`, `CustomExceptionTests`, `OutboxMessageTests`, `SeriesRepositoryTests`, `ProcessOutboxMessagesJobTests`, `DependencyInjectionTests`, `SwaggerInfrastructureTests`, `QueryablePaginationExtensionsTests` ve `InsertOutboxMessagesInterceptorTests` yazılarak toplam test sayısı **171'e** çıkarıldı.
   - SonarQube analizi çalıştırıldı: **Quality Gate: PASSED (GREEN)**, **Coverage: %90.2**, **0 Bug**, **0 Vulnerability**, **0 Code Smell** ve **%100 Security Hotspot Review** başarıyla tescillendi.
6. **Kod Stili ve İsimlendirme Kuralları Standardizasyonu (IDE1006 & dotnet format):**
   - `.editorconfig` ve `Directory.Build.props` üzerinden Roslyn kuralları (IDE0005 kullanılmayan using'lerin temizlenmesi, IDE1006 isimlendirme standartları, IDE0130 klasör/namespace eşleşmesi) derleme ve CI/CD kapılarına bağlandı.
   - `dotnet format --verify-no-changes` ve `dotnet build` ile arayüzler (`I` prefix), tipler (PascalCase), private alanlar (`_` prefix camelCase) ve parametrelerin kurumsal standartlara uyumu otomatik güvenceye alındı.

---

## 2. Neden Yaptık? (Mimari Kararlar ve Rasyoneller)

### 2.1 Konteyner Güvenliği (Non-Root User)
* **Problem:** Docker konteynerleri varsayılan olarak `root` yetkileriyle ayağa kalkar. Uygulama içerisinde bir uzaktan kod çalıştırma (RCE) zafiyeti oluşursa saldırgan doğrudan host sistem üzerinde root erişimi elde edebilir.
* **Karar:** .NET 8/9/10 resmi runtime imajlarında hazır gelen düşük yetkili `app` (UID: 1654) kullanıcısı `USER app` direktifi ile aktif hale getirildi.

### 2.2 Sır Yönetimi ve Konfigürasyon İzolasyonu
* **Problem:** Konfigürasyon dosyalarına yazılan IP, domain ve adresler ortamlar (Local, Staging, Prod) arasında taşınırken sızıntılara ve yapılandırma hatalarına yol açar.
* **Karar:** Vault adresi ve token'ları `appsettings.json` içerisinden tamamen arındırıldı. Docker Compose ve Kubernetes ortamlarında 12-Factor App manifestosuna uygun olarak `.env` ve ortam değişkenleri tek gerçek kaynak (Source of Truth) kılındı.

### 2.3 DDD BaseEntity Eşitlik Rasyoneli
* **Problem:** Entity sınıflarında `==` ve `!=` operatörlerinin aşırı yüklenmesi, EF Core proxy nesneleri, LINQ expression tree derlemeleri ve referans karşılaştırmalarında beklenmeyen davranışlara yol açabilmektedir.
* **Karar:** Kimlik tabanlı eşitlik `Equals(object?)` ve `GetHashCode()` metodları üzerinden sürdürüldü; Microsoft Framework Design Guidelines (S3875) ile %100 uyum sağlandı.

### 2.4 Domain Event Snapshotting ve Outbox Tutarlılığı
* **Problem:** `AggregateRoot.GetDomainEvents()` metodu `_domainEvents.AsReadOnly()` dönmektedir. Eğer LINQ zinciri içinde `aggregateRoot.ClearDomainEvents()` çağrılırsa, altta yatan liste temizlendiği için deferred execution sırasında `ReadOnlyCollection` boşalmakta ve Outbox mesajları veritabanına yazılamamaktadır.
* **Karar:** `InsertOutboxMessagesInterceptor` içinde `GetDomainEvents().ToList()` çağrısı ile olayların bellek kopyası (snapshot) alındıktan sonra liste temizlenmektedir.

---

## 3. Hangi Pattern'leri Kullandık?

1. **Transactional Outbox Pattern:**
   - Domain event'lerini ilişkisel veritabanı transaction'ı içerisinde `OutboxMessages` tablosuna atomik olarak yazar (`InsertOutboxMessagesInterceptor`).
   - Quartz tabanlı arka plan işi (`ProcessOutboxMessagesJob`) bu mesajları periyodik olarak okuyup MassTransit üzerinden RabbitMQ'ya güvenle yayınlar.
2. **Fail-Fast & Input Validation (Pipeline Behavior):**
   - FluentValidation ve MediatR `ValidationBehavior` ile hatalı komutlar daha Handler'a ulaşmadan 400 Bad Request yanıtına dönüştürülür.
3. **Repository Pattern & Specification/Filter:**
   - EF Core bağımlılığı repository arayüzleri ardına soyutlanmış; Soft-delete filtreleri global query filter düzeyinde otomatik yönetilmiştir.
4. **Standardized Problem Details (RFC 7231 / RFC 7807):**
   - Hata yönetiminde `GlobalExceptionHandler` ile iç stack trace gizlenerek standart JSON problem detayları üretilir.

---

## 4. Test Stratejisi ve Doğrulama

### 4.1 Test Suite Yapısı
- **Birim Testleri (Unit Tests):** Domain entity mantığı, FluentValidation kuralları, MediatR pipeline davranışları, Vault configuration provider HTTP mock'ları.
- **Entegrasyon Testleri (Integration Tests):** EF Core InMemory veritabanı üzerinde repository soft-delete davranışları, interceptor tetiklenmeleri ve asenkron sayfalama extension'ları.

### 4.2 Testleri Çalıştırma
```bash
# Tüm testleri çalıştırma (171 test)
dotnet test

# Kod kapsaması toplayarak test çalıştırma
dotnet test --settings coverage.runsettings --collect:"XPlat Code Coverage"

# Kod stil ve format doğrulama
dotnet format --verify-no-changes
```

### 4.3 Yerel SonarQube Analizi Çalıştırma
```bash
SONAR_TOKEN="<your-token>" ./scripts/run-sonar.sh
```

### 4.4 Kalite Kapısı (Quality Gate) Sonuçları
- **Status:** `PASSED` (`OK`)
- **New Code Coverage:** `%90.2` (Hedef: >= %80)
- **Bugs:** `0`
- **Vulnerabilities:** `0`
- **Code Smells:** `0`
- **Security Hotspots:** `%100 Reviewed`
- **Duplicated Lines:** `%0.0`
