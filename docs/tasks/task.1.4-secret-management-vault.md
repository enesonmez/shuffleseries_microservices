# Task 1.4: Secret Management (HashiCorp Vault) Entegrasyonu - Eğitim ve Mimari Dokümantasyonu

Bu doküman, ShuffleSeries mikroservis ekosisteminde merkezi sır yönetimi (Secret Management) amacıyla **HashiCorp Vault** altyapısının kurulmasını, `ShuffleSeries.Shared.Core.Infrastructure` altında özel `.NET Configuration Provider` geliştirilmesini ve OWASP API8 standartları doğrultusunda konfigürasyon dosyalarından hassas verilerin arındırılmasını açıklar.

---

## 1. Ne Yaptık? (Genel Özet)

1. **HashiCorp Vault Altyapısı (`docker-compose.yml`):**
   * `hashicorp/vault:1.17` servisi dev modunda (`server -dev`), `IPC_LOCK` yetkisi ve HTTP sağlık kontrolü (`vault status -address=http://127.0.0.1:8200`) ile ayağa kaldırıldı.
   * `vault_init` servisi ve [`docker/vault/init-vault.sh`](../../docker/vault/init-vault.sh) script'i ile geliştirici ortamı başlatıldığında paylaşılan (`shuffleseries/shared`) ve servise özel (`shuffleseries/catalog`) sırların otomatik olarak Vault KV-v2 secret engine'ine yazılması sağlandı.
2. **Özel .NET Configuration Provider (`Shared.Core.Infrastructure`):**
   * [`VaultConfigurationProvider`](../../ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Infrastructure/Configuration/Vault/VaultConfigurationProvider.cs): Vault HTTP API'sinden (`v1/{mount}/data/{path}`) KV-v2 verilerini çeken, hem düz anahtarları (`ConnectionStrings:Database`, `Redis__Password`) hem de iç içe geçmiş (nested) JSON nesnelerini hiyerarşik `.NET IConfiguration` sözlüğüne dönüştüren sağlayıcı yazıldı.
   * [`VaultConfigurationSource`](../../ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Infrastructure/Configuration/Vault/VaultConfigurationSource.cs) ve [`VaultExtensions`](../../ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Infrastructure/Configuration/Vault/VaultExtensions.cs): `builder.Configuration.AddVault("catalog")` çağrısı ile bootstrap aşamasında sırların enjekte edilmesini sağlayan genişletme metotları geliştirildi.
3. **Sırların Kaynak Koddan Arındırılması (Sanitization - OWASP API8):**
   * `ShuffleSeries.Catalog.Api/appsettings.json` ve `appsettings.Development.json` dosyalarından veritabanı şifresi ve RabbitMQ kullanıcı bilgileri tamamen kaldırıldı. Sadece Vault adres ve mount ayarları bırakıldı.
   * `Catalog.Api/Program.cs` servisi bootstrap anında `builder.Configuration.AddVault("catalog")` ile sırları dinamik olarak Vault'tan okuyacak şekilde güncellendi.
4. **Kapsamlı Test Altyapısı:**
   * `ShuffleSeries.Shared.Core.Tests` altına `VaultConfigurationProviderTests` (6 yeni unit test) eklendi; parsing, flattening, token doğrulama, 404 fallback ve network hata durumları test edildi. Toplam test sayısı 77'ye ulaştı (%100 başarı).

---

## 2. Neden Yaptık? (Mimari Kararlar ve "Neden?" Analizi)

### Soru 1: Neden veritabanı şifrelerini ve RabbitMQ kimlik bilgilerini `appsettings.json` içinde tutmuyoruz?
* **Problem (OWASP API8 - Security Misconfiguration / Secret Sprawl):** Yapılandırma dosyalarında hardcoded tutulan sırlar, Git reposuna commit edilme, geliştiricilerin makinelerinde açık metin olarak kalma ve container imajlarının katmanlarına (image layers) sızma riski taşır.
* **Karar:** 12-Factor App ve kurumsal güvenlik standartlarına uygun olarak tüm hassas veriler merkezi bir Secret Store'a (HashiCorp Vault) taşındı. Geliştirici ortamında bile sırlar kaynak kodun parçası olamaz.

### Soru 2: Neden üçüncü parti bir Vault SDK (örn. `VaultSharp`) yerine kendi `HttpClient` tabanlı Configuration Provider'ımızı yazdık?
* **Problem:** Harici üçüncü parti kütüphaneler .NET 10 gibi en yeni platform sürümlerinde bağımlılık zincirlerinde (dependency hell) uyumsuzluk yaratabilir veya ilerleyen sürümlerde lisans değişikliklerine (MassTransit v9 örneğinde yaşandığı gibi) uğrayabilir.
* **Karar:** HashiCorp Vault'un KV-v2 HTTP API'si oldukça basit, standart ve RESTful'dur (`GET /v1/secret/data/{path}` + `X-Vault-Token`). .NET'in yerel `HttpClient` ve `JsonDocument` sınıfları ile yazılan sağlayıcı; sıfır harici paket yükü, yüksek bellek verimliliği, tam şeffaflık ve mock'lanabilir test edilebilirlik sağlar.

### Soru 3: Neden hiyerarşik sır yolu (`shuffleseries/shared` ve `shuffleseries/{serviceName}`) kullandık?
* **Problem:** Mikroservislerin kullandığı bazı ayarlar (RabbitMQ sunucusu, Redis bağlantısı, JWT Secret) tüm servisler için ortaktır; bazıları ise (PostgreSQL connection string) sadece tek bir servise aittir. Hepsini tek bir yola yığmak izolasyonu bozar; her servis için ayrı ayrı çoğaltmak ise DRY prensibine aykırıdır.
* **Karar:** 
  * `shuffleseries/shared`: Tüm mikroservisler tarafından ortak okunur.
  * `shuffleseries/{serviceName}`: İlgili servisin Bounded Context'ine özeldir. `AddVault("catalog")` metodu bu iki yolu sırayla okuyup birleştirir.

---

## 3. Hangi Pattern'leri ve Prensipleri Kullandık?

1. **Configuration Provider Pattern:**
   * .NET'in genişletilebilir konfigürasyon altyapısına (`IConfigurationSource`, `ConfigurationProvider`) uyularak uygulama koduna hiçbir Vault bağımlılığı sızdırılmadı. Domain ve Application katmanları ayarların Vault'tan mı yoksa appsettings'ten mi geldiğinden habersizdir (Separation of Concerns).
2. **Security by Design & Principle of Least Privilege:**
   * Servisler sadece yetkili oldukları mount point ve sır yollarına erişebilir.
3. **Fallback & Graceful Degradation Pattern:**
   * `VaultConfigurationOptions.Optional = true` seçeneği ile Vault'a erişilemediğinde (örneğin izole unit test ortamlarında veya lokal Docker çalışmadığında) uygulamanın anında çökmesi engellenir; ortam değişkenleri veya lokal yedek değerlerle devam edebilmesi sağlanır.
4. **Idempotent Automation (Seeder Pattern):**
   * `init-vault.sh` script'i idempotent çalışır; sırları üzerine yazar (`vault kv put`) ve hata durumunda sistemi bloke etmez.

---

## 4. Yapılan Geliştirme Nasıl Test Edilir?

### Test Senaryosu 1: Vault API Doğrudan Sorgulama
```bash
# Shared sırlarını doğrula
curl -s -H "X-Vault-Token: root" http://localhost:8200/v1/secret/data/shuffleseries/shared | jq .data.data

# Catalog sırlarını doğrula
curl -s -H "X-Vault-Token: root" http://localhost:8200/v1/secret/data/shuffleseries/catalog | jq .data.data
```
**Beklenen Sonuç:**
* Shared altında `MessageBroker:Host`, `MessageBroker:Username`, `Redis:Password` vb.
* Catalog altında `ConnectionStrings:Database` dönmelidir.

### Test Senaryosu 2: Unit Testlerin Çalıştırılması
```bash
dotnet test --filter "FullyQualifiedName~VaultConfigurationProviderTests"
```
**Beklenen Sonuç:** 6 testin tamamı başarılı (`Passed`) olmalıdır.

### Test Senaryosu 3: Catalog API'nin Sırları Vault'tan Okuyarak Başlatılması
```bash
dotnet run --project ShuffleSeries.Catalog/ShuffleSeries.Catalog.Api
```
**Beklenen Sonuç:**
* `appsettings.json` içinde şifre bulunmamasına rağmen uygulama Vault'tan connection string ve RabbitMQ şifrelerini çeker.
* PostgreSQL veritabanına bağlanır ve migration'ları uygular.
* RabbitMQ'ya bağlanarak MassTransit bus'ını ayağa kaldırır.
* Quartz Outbox scheduler'ını başlatır.
