# Refactor & Feature: 3 Ayaklı Mimari Savunma ve Deterministik Scaffolding Sistemi

## 1. Ne Yaptık?
Catalog ve Identity mikroservisleri arasındaki asimetrilerin giderilmesinin ardından, gelecekte geliştirilecek yeni Bounded Context'lerde (örn. `ShuffleEngine`, `UserLibrary`, `History`) benzer mimari sapmaların ve asimetrilerin yaşanmasını %100 engelleyen **3 Ayaklı Savunma Sistemi (Defense-in-Depth Governance)** kuruldu:

1. **Kural & Rehber (Declarative Guard):**
   * `.agents/rules/architecture-standard.md` dosyası oluşturuldu. 5 katmanlı Onion Architecture (Domain, Application, Infrastructure, Api, Tests, IntegrationTests), 1-Class-1-File kuralı, SUT klasör aynalama ve `Program.cs` pipeline standardı yazılı anayasaya bağlandı.
   * `AGENTS.md` içerisine demir kural olarak entegre edildi.

2. **Evrensel Mimari Test Muhafızı (Architectural Fitness Base):**
   * `ShuffleSeries.ArchitectureTests/Common/MicroserviceArchitectureTestBase.cs` soyut taban sınıfı yazıldı.
   * Katman bağımlılıkları, CQRS kısıtları, sealed handler/validator kuralları, Domain encapsulation, repository CancellationToken kontrolleri ve Quartz job kuralları bu ortak sınıfta toplandı.
   * `CatalogArchitectureTests` (~440 satırdan 60 satıra) ve `IdentityArchitectureTests` (~410 satırdan 45 satıra) bu taban sınıftan türetilerek sıfır kod tekrarı ile refactor edildi.
   * Bu entegrasyon sırasında Identity'deki 3 adet mühürlenmemiş validator (`RegisterCommandValidator`, `LoginCommandValidator`, `RefreshTokenCommandValidator`) anında yakalanarak `sealed` yapıldı.

3. **Deterministik Scaffolding Motoru (Automated CLI):**
   * `scripts/scaffold_service.py` CLI aracı geliştirildi.
   * `python3 scripts/scaffold_service.py <ServiceName> [--port <Port>] [--dry-run]` komutu ile 4 Onion katmanı + 2 test projesi, tam `.csproj` paket/proje referansları, DI extension'ları, standart `Program.cs`, OpenAPI konfigürasyonu, launchSettings, Dockerfile ve `ArchitectureTests` entegrasyonu 2 saniyede sıfır hata ile üretilir hale getirildi.
   * Dummy bir servis (`DummyScaffold`) ile uçtan uca test edildi; 0 hata ile derlendiği, 23 mimari testin ve 4 unit testin doğrudan geçtiği kanıtlandıktan sonra güvenle geri alındı (rollback).

---

## 2. Neden Yaptık?
* **Pasif Dokümantasyon Zaafiyeti:** Kurallar yalnızca metin olarak kaldığında geliştirici veya AI agent 1-class-1-file kuralını unutabilir, event'leri tek dosyada toplayabilir veya SUT test aynalamasını atlayabilir.
* **Manuel Kurulum Maliyeti ve Sapma:** Yeni bir mikroservis oluşturulurken `.csproj` bağımlılıkları, NuGet paket versiyonları ve `launchSettings.json` portları elle yapılandırıldığında mikroservisler arası asimetri kaçınılmaz hale gelir.
* **Kural Paritesi Garantisi:** Mimari testlerin her serviste ayrı ayrı kopyalanması kural eksikliğine yol açıyordu (Identity'de daha önce 15 kuralın eksik olması gibi). Soyut taban sınıf ile yeni bir servis sadece 5 satır tanımla tüm mimari korumalara kavuşur.

---

## 3. Hangi Pattern ve Prensipleri Kullandık?
* **Onion Architecture:** Bağımlılıkların dıştan içe aktığı, Domain'in dış dünyadan ve framework'lerden %100 izole olduğu mimari model.
* **Architectural Fitness Functions (NetArchTest.Rules):** Mimari sınırların, katman izolasyonunun ve tasarım desenlerinin kodla sürekli test edilmesi.
* **Template Method Pattern:** `MicroserviceArchitectureTestBase` içerisinde test metodları tanımlanırken hedef Bounded Context'e özgü değişkenlerin (`DomainAssembly`, `ServiceNamespacePrefix`, `AggregateRootTypes`) türeyen sınıflarca sağlanması.
* **Single Source of Truth (SSOT):** NuGet versiyonları, klasör anatomisi ve pipeline sıralamasının tek bir script ve kural dosyasında toplanması.
* **Clean Code & SUT Mirroring:** Test klasörlerinin kaynak kod klasörlerini $1:1$ aynalaması.

---

## 4. Nasıl Test Edilir?

### 4.1. Mimari Testlerin Doğrulanması
```bash
dotnet test ShuffleSeries.ArchitectureTests/ShuffleSeries.ArchitectureTests.csproj
```
**Beklenen Çıktı:** 53 testin tamamı (Catalog, Identity ve Shared.Core) %100 başarılı.

### 4.2. Scaffolding Motorunun Test Edilmesi (Dry-Run)
```bash
python3 scripts/scaffold_service.py ShuffleEngine --dry-run
```
**Beklenen Çıktı:** 35 dosyalık tam Onion + Test ağacının simülasyonu.

### 4.3. Yeni Bir Servisin Otomatik Üretilmesi
```bash
python3 scripts/scaffold_service.py ShuffleEngine --port 5003
dotnet build
dotnet test --filter FullyQualifiedName~ShuffleEngine
```
**Beklenen Çıktı:** Servis 0 hata ile derlenir, 23 mimari test ve tüm unit testler doğrudan geçer.
