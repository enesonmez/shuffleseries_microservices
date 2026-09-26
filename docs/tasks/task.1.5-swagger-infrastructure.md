# Task 1.5: OpenAPI, Swagger UI ve Scalar Dokümantasyon Altyapısı - Eğitim ve Mimari Dokümantasyonu

Bu doküman, ShuffleSeries mikroservis ekosisteminde merkezi, yeniden kullanılabilir ve standartlaştırılmış API dokümantasyonu altyapısının (`ShuffleSeries.Shared.Core.Web` altında) kurulumunu, .NET 10 yerel OpenAPI desteği (`Microsoft.AspNetCore.OpenApi`), modern Swagger UI (`Swashbuckle.AspNetCore.SwaggerUI`), yeni nesil API dokümantasyon arayüzü **Scalar** (`Scalar.AspNetCore`), interaktif JWT Bearer yetkilendirme şemasını ve `Catalog.Api` Minimal API endpoint'lerinin zenginleştirilmesini açıklar.

---

## 1. Ne Yaptık? (Genel Özet)

1. **Merkezi Swagger & OpenAPI Yapılandırması (`Shared.Core.Web`):**
   * [`SwaggerOptions`](../../ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Web/Swagger/SwaggerOptions.cs): API başlığı, sürümü, açıklaması, rota ön eki (`RoutePrefix`), JWT yetkilendirme bayrağı (`IncludeJwtSecurity`) ve arayüz açma/kapama bayrakları (`EnableOpenApi`, `EnableSwaggerUi`, `EnableScalarUi`) içeren tip güvenli ayar sınıfı oluşturuldu.
   * [`OpenApiSecurityDocumentTransformer`](../../ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Web/Swagger/OpenApiSecurityDocumentTransformer.cs): .NET 10 yerel OpenAPI altyapısı için `IOpenApiDocumentTransformer` arayüzünü uygulayarak, OpenAPI dokümanına API metadata'sını (`OpenApiInfo`), JWT Bearer güvenlik şemasını (`Bearer` HTTP scheme, JWT format) ve global güvenlik gereksinimini (`OpenApiSecurityRequirement`) dinamik olarak enjekte eden transformer geliştirildi.
   * [`SwaggerExtensions`](../../ShuffleSeries.Shared.Core/ShuffleSeries.Shared.Core.Web/Swagger/SwaggerExtensions.cs): `builder.Services.AddSharedSwagger(...)` ve `app.UseSharedSwagger()` extension metotları yazılarak herhangi bir mikroservisin tek satırla hem OpenAPI JSON dokümanını (`/openapi/v1.json`), hem klasik Swagger UI'ı (`/swagger`), hem de modern Scalar UI'ı (`/scalar/v1`) sunması sağlandı.
2. **Catalog Mikroservisi Entegrasyonu (`Catalog.Api`):**
   * [`Program.cs`](../../ShuffleSeries.Catalog/ShuffleSeries.Catalog.Api/Program.cs): Servis açılışında `AddSharedSwagger()` ve middleware hattında `UseSharedSwagger()` entegre edildi.
   * [`SeriesEndpoints.cs`](../../ShuffleSeries.Catalog/ShuffleSeries.Catalog.Api/Endpoints/SeriesEndpoints.cs): Tüm Minimal API rotaları `.WithTags("Series")`, `.WithSummary(...)`, `.WithDescription(...)`, `.Produces<T>(...)`, `.ProducesValidationProblem()` ve `.ProducesProblem(...)` metadata'ları ile zenginleştirilerek uçtan uca tip güvenli OpenAPI şeması üretildi.
3. **NuGet Güvenlik ve Bağımlılık Optimizasyonu:**
   * `Microsoft.OpenApi` kütüphanesinin bilinen NU1903 (orta düzey güvenlik açığı bulunan 2.0.0 sürümü) zafiyeti, hem `Shared.Core.Web` hem de `ApiGateway` projelerinde doğrudan güvenli `2.12.2` sürümüne yükseltilerek bertaraf edildi.
4. **Kapsamlı Test Kapsamı:**
   * `ShuffleSeries.Shared.Core.Tests` altına `SwaggerInfrastructureTests` (5 yeni unit test) eklendi; metadata enjeksiyonu, JWT security açık/kapalı durumları, default opsiyonlar ve servis koleksiyonu kaydı test edildi. Çözümdeki toplam test sayısı 82'ye (%100 başarı) yükseltildi.

---

## 2. Neden Yaptık? (Mimari Kararlar ve "Neden?" Analizi)

### Soru 1: Neden tek başına Swashbuckle veya NSwag kullanmak yerine .NET 10 yerel `Microsoft.AspNetCore.OpenApi` desteğini tercih ettik?
* **Problem:** Swashbuckle kütüphanesi uzun süre aktif bakım almamış, topluluk tarafından çatallanmış (fork) ve .NET 8/9/10 ile gelen AOT (Ahead-of-Time compilation), Minimal API Endpoint Metadata ve yüksek performanslı System.Text.Json serileştirme modellerine uyum sağlamakta zorlanmıştır.
* **Karar:** .NET 9 ve .NET 10 ile birlikte Microsoft, resmi ve yüksek performanslı OpenAPI v3 doküman üretim motorunu (`Microsoft.AspNetCore.OpenApi`) doğrudan framework'e dahil etti. Doküman üretimini resmi motor ile yapıp, arayüz olarak yalnızca UI katmanlarını (`SwaggerUI` ve `Scalar`) bağlamak hem geleceğe dönük (future-proof) hem de AOT-ready bir mimari sunar.

### Soru 2: Neden hem Swagger UI hem de Scalar API Reference desteği sunduk?
* **Problem:** Klasik Swagger UI yıllardır endüstri standardı olsa da, karmaşık mikroservis ekosistemlerinde büyük şemaları render ederken yavaş kalabilmekte ve modern geliştirici deneyimi (dark mode, çoklu dil kod örnekleri, interaktif cURL/SDK kopyalama) sunamamaktadır.
* **Karar:** 
  * **Swagger UI (`/swagger`):** QA mühendisleri, frontend geliştiriciler ve ekosistemin alışık olduğu geleneksel arayüz olarak korunur.
  * **Scalar (`/scalar/v1`):** Açık kaynaklı, inanılmaz derecede hızlı, modern UI ve otomatik istek örnekleri üreten yeni nesil dokümantasyon deneyimi sunar.
  * İki arayüz de aynı `/openapi/v1.json` kontratını tüketir; sıfır ek maliyetle geliştiricilere arayüz seçme esnekliği tanınır.

### Soru 3: Neden JWT Bearer güvenlik şemasını `OpenApiDocumentTransformer` ile enjekte ettik?
* **Problem:** Mikroservislerimizin çoğu Identity servisi tarafından üretilen JWT token'lar ile korunacaktır. Swagger/Scalar arayüzlerinde `Authorize` butonunun olmaması, geliştiricilerin korumalı endpoint'leri test etmek için harici Postman/cURL araçlarına yönelmesine neden olur.
* **Karar:** `IOpenApiDocumentTransformer` kullanılarak, OpenAPI standardına tam uyumlu `Http` tipinde `Bearer` şeması dokümana otomatik eklenir. `IncludeJwtSecurity` bayrağı ile istenirse tamamen public servislerde bu şema tek bir satırla kapatılabilir.

---

## 3. Hangi Pattern'leri ve Prensipleri Kullandık?

1. **Document Transformer Pattern (`IOpenApiDocumentTransformer`):**
   * OpenAPI spesifikasyonunun oluşturulması sırasında araya girerek (interception) şemayı manipüle eden, metadata ve güvenlik kuralları ekleyen resmi genişletme deseni kullanıldı.
2. **Options Pattern (`IOptions<SwaggerOptions>`):**
   * Yapılandırma ayarları `appsettings.json` veya Fluent Action (`options => { ... }`) üzerinden tip güvenli olarak enjekte edilebilir.
3. **Extension Method & Facade Pattern:**
   * Karmaşık OpenAPI ve UI kayıt zincirleri `AddSharedSwagger()` ve `UseSharedSwagger()` metotları arkasında soyutlandı. Mikroservislerin `Program.cs` dosyalarında konfigürasyon kirliliği önlendi.
4. **Self-Documenting Code (Clean Architecture API Design):**
   * Minimal API endpoint'leri `Produces<T>`, `ProducesValidationProblem`, `WithSummary` ve `WithDescription` ile kodun kendisi üzerinden canlı kontrat (living contract) haline getirildi.

---

## 4. Yapılan Geliştirme Nasıl Test Edilir?

### Test Senaryosu 1: Birim Testlerin Doğrulanması
```bash
dotnet test --filter "FullyQualifiedName~SwaggerInfrastructureTests"
```
**Beklenen Çıktı:** 5 testin tamamı (Metadata enjeksiyonu, Bearer şeması, JWT pasif durumu, varsayılan ayarlar) başarıyla geçmelidir.

### Test Senaryosu 2: OpenAPI JSON Kontratının Doğrulanması
Catalog servisi ayağa kaldırıldığında:
```bash
curl -s http://localhost:5000/openapi/v1.json | jq .info
```
**Beklenen Çıktı:**
```json
{
  "title": "ShuffleSeries Catalog API",
  "version": "v1",
  "description": "ShuffleSeries Content Catalog Microservice - Movies, Series, Episodes and Streaming Platforms"
}
```

Aynı zamanda dokümanda Bearer güvenlik şeması bulunmalıdır:
```bash
curl -s http://localhost:5000/openapi/v1.json | jq .components.securitySchemes
```

### Test Senaryosu 3: Swagger UI ve Scalar Arayüzleri
Tarayıcıda aşağıdaki adresler açılır:
* **Swagger UI:** `http://localhost:5000/swagger` -> Klasik Swagger arayüzü ve sağ üstte "Authorize" butonu görüntülenmelidir.
* **Scalar API Reference:** `http://localhost:5000/scalar/v1` -> Modern, karanlık mod destekli interaktif API dokümantasyonu açılmalıdır.
