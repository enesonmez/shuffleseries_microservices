# Task 1.6: CI/CD Pipeline (GitHub Actions) ve SonarQube Entegrasyonu - Eğitim ve Mimari Dokümantasyonu

Bu doküman, ShuffleSeries mikroservis ekosisteminde sürekli entegrasyon ve sürekli dağıtım (**Continuous Integration / Continuous Delivery - CI/CD**) süreçlerinin **GitHub Actions** üzerinde kurulmasını, kod kalitesi ve güvenlik analizleri için **SonarQube / SonarCloud** entegrasyonunu, otomatik test kapsamı (**Code Coverage**) raporlamasını ve **Docker Container Build Integrity Gate** mimarisini açıklar.

---

## 1. Ne Yaptık? (Genel Özet)

1. **GitHub Actions CI/CD Pipeline Yapılandırması ([`.github/workflows/ci.yml`](../../.github/workflows/ci.yml)):**
   * `master` ve `main` dallarına yapılan tüm `push` ve `pull_request` olaylarında, ayrıca `workflow_dispatch` ile manuel tetiklenen kurumsal seviyede bir GitHub Actions iş akışı (workflow) oluşturuldu.
2. **Çift Aşamalı İş Akışı (Multi-Job Workflow Architecture):**
   * **`build-and-test` İşi:**
     * **Java 21 LTS** (`actions/setup-java@v4`) ve **.NET 10 SDK** (`actions/setup-dotnet@v4`) ortamları kuruldu.
     * **NuGet Önbellekleme (`actions/cache@v4`):** `.csproj` ve `.props` dosya hash'lerine göre `~/.nuget/packages` dizini önbelleğe alınarak pipeline çalışma süresi minimize edildi.
     * **Kod Standartları Kapısı (Code Style Gate):** `dotnet format --verify-no-changes` adımı ile Clean Code ve format kurallarına uymayan pull request'lerin erken aşamada reddedilmesi sağlandı.
     * **SonarQube / SonarCloud Analizi:** `dotnet-sonarscanner` aracı ile güvenlik açıkları (Vulnerabilities), kod kokuları (Code Smells), potansiyel hatalar (Bugs) ve teknik borç analizi başlatıldı.
     * **Derleme & Test:** `dotnet build -c Release` sonrasında çözümdeki 105 testin tamamı çalıştırıldı.
     * **Code Coverage (Cobertura & OpenCover):** `coverlet.collector` kullanılarak XPlat Code Coverage formatında test kapsama raporları üretildi ve test sonuçları (`.trx`) ile birlikte GitHub Actions Artifacts (`test-results-and-coverage`) olarak 14 gün saklanmak üzere yüklendi.
     * **Graceful Degradation (Esnek Geri Çekilme):** Eğer depoda `SONAR_TOKEN` secret'ı henüz tanımlanmamışsa pipeline çökmez; derleme, format ve test adımlarını başarıyla tamamlar.
   * **`docker-verification` İşi:**
     * `build-and-test` işi başarıyla bittikten sonra (`needs: build-and-test`) tetiklenir.
     * `ShuffleSeries.Catalog.Api/Dockerfile` ve `ShuffleSeries.ApiGateway/Dockerfile` dosyalarını derleyerek mikroservis container imajlarının kırılmadığını (Container Integrity) doğrular.

---

## 2. Neden Yaptık? (Mimari Kararlar ve "Neden?" Analizi)

### Soru 1: Neden CI pipeline'ında .NET dışında Java 21 kuruyoruz?
* **Problem:** `dotnet-sonarscanner` CLI aracı, C# kodunu analiz ederken arka planda Java tabanlı Roslyn Sonar analyzer motorunu çalıştırır. Runner makinesinde Java yüklü olmadığında analiz motoru başlatılamaz.
* **Karar:** `actions/setup-java@v4` ile resmi Temurin Java 21 LTS runtime'ı kurularak SonarScanner'ın en yüksek performans ve uyumlulukla çalışması sağlandı.

### Soru 2: Neden `fetch-depth: 0` kullanıyoruz?
* **Problem:** GitHub Actions varsayılan `checkout` adımında disk ve ağ tasarrufu için yalnızca tek bir commit (`shallow clone`, `fetch-depth: 1`) çeker. Ancak SonarQube, kod satırlarının hangi commit ve geliştirici tarafından yazıldığını (Git blame) ve yeni kod (New Code Period) farklarını analiz edebilmek için tüm git geçmişine ihtiyaç duyar.
* **Karar:** `fetch-depth: 0` verilerek SonarQube'ün pull request analizlerinde tam doğrulukla çalışması sağlandı.

### Soru 3: Neden Docker imaj derlemesini CI aşamasında ayrı bir job olarak doğruluyoruz?
* **Problem:** Yerel ortamda `dotnet run` ile derlenen ve çalışan bir proje; eksik bir `.dockerignore` istisnası, Dockerfile içerisindeki yanlış bir dosya yolu (`COPY`) veya multi-stage build hatası nedeniyle container ortamında derlenemeyebilir. Bu durum genellikle canlıya (production) çıkış anında keşfedilir ve acil durum kesintilerine yol açar.
* **Karar:** CI sürecinde `docker build` adımı zorunlu bir kalite kapısı (Quality Gate) haline getirilerek, container imajı üretilemeyen hiçbir kodun ana dala (`master`) girmesine izin verilmez.

### Soru 4: Neden SonarQube token kontrolünde `if: env.SONAR_TOKEN != ''` koşulu kullandık?
* **Problem:** Açık kaynaklı repolarda, forklanmış repolarda veya secret'ların henüz tanımlanmadığı ilk geliştirme aşamalarında `SONAR_TOKEN` bulunmadığında pipeline hata verip tüm derleme ve testleri bloke edebilir.
* **Karar:** "Graceful Fallback" deseni ile token yoksa Sonar analizi atlanır; ancak derleme, format kontrolü ve tüm unit testler çalışmaya devam eder. Token eklendiği anda SonarQube otomatik olarak devreye girer.

---

## 3. Hangi Pattern'leri ve Prensipleri Kullandık?

1. **Quality Gate Pattern:**
   * Kodun canlıya gitmeden önce birden fazla bağımsız filtreyi (Format -> Compile -> Test -> Code Coverage -> Security Scan -> Docker Build) sırayla geçmesi zorunlu kılınmıştır.
2. **Fail-Fast Prensibi:**
   * En ucuz ve en hızlı adım olan `dotnet format --verify-no-changes` en başa konulmuştur. Kod stili bozuksa dakikalarca derleme veya Docker beklemeden anında saniyeler içinde hata döner.
3. **Defense in Depth & DevSecOps:**
   * Sırlar hardcoded tutulmaz (`secrets.SONAR_TOKEN`); kod kalitesi ve OWASP açıkları SonarQube ile her commit'te otomatik taranır.
4. **Build Once, Verify Anywhere (Container Integrity):**
   * Container imajları üretim anından önce CI ortamında derlenip doğrulanır.

---

## 4. Yapılan Geliştirme Nasıl Test Edilir?

### Test Senaryosu 1: Yerel Format Doğrulaması
```bash
dotnet format --verify-no-changes
```
**Beklenen Sonuç:** Exit code 0 dönmeli, biçimlendirme hatası olmamalıdır.

### Test Senaryosu 2: Yerel Test ve Kapsama (Coverage) Üretimi
```bash
dotnet test -c Release --collect:"XPlat Code Coverage" --results-directory ./TestResults
```
**Beklenen Sonuç:** 105 testin tamamı başarılı olmalı ve `./TestResults/` altında `coverage.cobertura.xml` dosyaları üretilmelidir.

### Test Senaryosu 3: Docker İmajlarının Yerel Derlenmesi
```bash
docker build -f ShuffleSeries.Catalog/ShuffleSeries.Catalog.Api/Dockerfile -t shuffleseries-catalog:ci .
docker build -f ShuffleSeries.ApiGateway/Dockerfile -t shuffleseries-apigateway:ci .
```
**Beklenen Sonuç:** Her iki imaj da başarıyla derlenmelidir (`FINISHED`).

### Test Senaryosu 4: GitHub Actions Üzerinde Otomatik Çalışma
Kod GitHub'a push edildiğinde veya bir Pull Request açıldığında:
* GitHub deposundaki **Actions** sekmesi altında `CI/CD Pipeline` iş akışı otomatik olarak tetiklenir.
* `Build, Test & SonarQube Analysis` ve `Docker Container Build Verification` işleri yeşil (`Success`) olarak tamamlanır.
