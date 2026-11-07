# Task Guide: Wolverine Migration (MediatR & MassTransit ➔ WolverineFx Konsolidasyonu)

Bu doküman, ShuffleSeries mikroservis ekosisteminde CQRS (In-Memory Mediator), Pipeline Middleware'leri ve RabbitMQ mesajlaşma altyapısının **MediatR ve MassTransit ikilisinden bağımsız, modern, MIT lisanslı ve Roslyn Source Generator tabanlı WolverineFx mimarisine** dönüştürülmesini detaylandırır.

---

## 1. Ne Yaptık? (Uygulanan Geliştirmenin Genel Özeti)

1. **Bağımlılık Konsolidasyonu ve Tasfiyesi:**
   - Proje genelinden `MediatR`, `MediatR.Contracts` ve `MassTransit`, `MassTransit.RabbitMQ` bağımlılıkları tamamen kaldırıldı.
   - Yerine `WolverineFx`, `WolverineFx.RabbitMQ`, `WolverineFx.FluentValidation` ve `WolverineFx.RuntimeCompilation` paketleri entegre edildi.
2. **POCO CQRS Standartlaşması:**
   - Tüm Command ve Query nesnelerinden `IRequest<T>` arayüzü kaldırıldı; saf C# 14 `record` modellerine dönüştürüldü.
   - Tüm Handler sınıflarından `IRequestHandler<TRequest, TResponse>` arayüzü kaldırıldı; Wolverine'in derleme zamanı konvansiyonu olan `public Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)` imzası korundu.
   - Wolverine 6+ Roslyn kod üretim motorunun sınıflara erişebilmesi için tüm Handler'lar `public sealed class` standardına çekildi.
3. **Compile-Time Sıfır-Maliyetli (Zero-Alloc) Middleware Hattı:**
   - Eski MediatR runtime reflection tabanlı `ValidationBehavior`, `LoggingBehavior` ve `PerformanceBehavior` sınıfları tasfiye edildi.
   - Doğrulama Wolverine'in yerel `opts.UseFluentValidation()` kaynak-kod üreticisine devredildi.
   - `LoggingMiddleware` ve `PerformanceMiddleware`, `Envelope envelope` parametresiyle çalışan statik compile-time middleware sınıfları olarak `ShuffleSeries.Shared.Core.Application.Middlewares` altında yeniden inşa edildi.
   - PII maskeleme (`Password`, `Token`, `Secret` alanları), 500ms yavaş istek uyarıları ve hassas hata yakalama (`OnException` + `ExceptionDispatchInfo`) garanti altına alındı.
4. **Onion Architecture / Clean Architecture Katman Ayrımı:**
   - Application Katmanı: `opts.Add{Service}ApplicationMessaging()` metodu ile Handler keşfi, `UseFluentValidation()`, `LoggingMiddleware`, `PerformanceMiddleware` ve `opts.RestoreV5Defaults()` yapılandırıldı.
   - Infrastructure Katmanı: `opts.Add{Service}MessagingTransport(IConfiguration)` metodu ile RabbitMQ broker bağlantısı ve routing topolojisi yapılandırıldı.
   - Api Katmanı (`Program.cs`): Yalnızca 2 satırlık temiz Composition Root sağlandı.
5. **Yalın Hata Yönetimi (YAGNI & Ponytail):**
   - Mükerrer olan özel `ShuffleSeries.Shared.Core.Exceptions.ValidationException` sınıfı tamamen silindi.
   - `GlobalExceptionHandler`, Wolverine'in yerel olarak fırlattığı `FluentValidation.ValidationException`'ı doğrudan yakalayarak RFC 7807 ProblemDetails formatında döndürecek şekilde yalınlaştırıldı.
6. **Outbox Mesaj Yayınında Timeout ve Cancellation Koruması:**
   - `ProcessOutboxMessagesJob` içerisindeki `_messageBus.PublishAsync(...)` çağrısı, `.AsTask().WaitAsync(cancellationToken)` ile Quartz job'ının ve 10 saniyelik linked timeout token'ının kontrolüne bağlandı; RabbitMQ kilitlenmelerinde DB transaction'ının askıda kalması engellendi.
7. **Deterministik Scaffolding Güncellemesi:**
   - `scripts/scaffold_service.py` şablonları Wolverine çift katmanlı extension yapısına ve `WolverineFx.RuntimeCompilation` paketine güncellendi.
8. **Kapsamlı Test Doğrulaması:**
   - 922/922 Unit ve Mimari Test.
   - 8/8 Yeni Pipeline Entegrasyon Testi (`CatalogMessagingPipelineIntegrationTests`, `IdentityMessagingPipelineIntegrationTests`).
   - 126/126 Gateway E2E Testi (%100 Başarı).

---

## 2. Neden Yaptık? (Mimari Kararlar ve Tasarım Gerekçeleri)

### A. MediatR & MassTransit Neden Ayrıştırıldı ve Konsolide Edildi?
- **Çift Altyapı Karmaşıklığı:** Eski yapıda in-memory komutlar için `ISender`, kuyruk mesajları için ise `IPublishEndpoint` kullanılıyordu. Bu durum mikroservisler arasında kafa karışıklığına ve aşırı bağımlılık kütüphanesine yol açıyordu.
- **Lisanslama Modeli:** MassTransit ve MediatR ekosistemlerindeki lisans politikası değişiklikleri veya dual-license tartışmalarına karşı Wolverine **%100 MIT Lisanslıdır** ve kurumsal projelerde hiçbir hukuki kısıt doğurmaz.
- **Reflection vs Compile-Time Roslyn Generator:** MediatR çalışma zamanında reflection kullanarak handler ve pipeline behavior nesnelerini çözerken bellek ayırması (heap allocation) yapar. Wolverine ise handler ve middleware zincirlerini derleme anında (Roslyn Source Generator) statik C# koduna dönüştürür; sıfıra yakın bellek tahsisi ve Native AOT uyumluluğu sağlar.

### B. Neden `ValidationException` Silindi? (Ponytail Merdiveni)
Önceki yapıda `FluentValidation.ValidationException` yakalanıp projenin kendi `ValidationException`'ına dönüştürülüyor, o da `GlobalExceptionHandler`'da ele alınıyordu. Bu bir katmanlama kirliliğiydi (Over-Engineering). Wolverine doğrudan `FluentValidation.ValidationException` fırlattığı için, aradaki ara sınıf silindi ve `GlobalExceptionHandler` doğrudan FluentValidation istisnasını işleyerek RFC 7807 sözleşmesini eksiksiz üretti.

### C. Neden `RestoreV5Defaults()` Kullanıldı?
Wolverine v6+, varsayılan olarak `ServiceLocationPolicy.NotAllowed` kuralını benimser. Handler sınıflarının constructor'ında `internal` arayüzler veya DI üzerinden sağlanan dahili servisler (örn. `ICatalogUnitOfWork`, `IUserRepository`) bulunduğunda kod üreticisi `InvalidServiceLocationException` fırlatır. `opts.RestoreV5Defaults()` politikası, Wolverine'in dahili servisleri güvenli bir şekilde `IServiceProvider` üzerinden şeffafça çözümlemesine izin vererek Clean Architecture katman sınırlarını bozmadan çalışmasını sağlar.

### D. Neden Handler'lar `public sealed class` Yapıldı?
Wolverine'in Roslyn generator motoru (`WolverineHandlers`), handler sınıflarını dinamik olarak ayrı bir derleme bağlamında türetir ve çağırır. Wolverine 6.0+ HandlerDiscovery motoru kural olarak `Handler types must be public, concrete, and closed` şartını koşar. Bu bir güvenlik zafiyeti değildir; çünkü Minimal API endpoint'leri hiçbir zaman handler sınıflarını doğrudan enjekte etmez, sadece `IMessageBus`'ı kullanır.

---

## 3. Hangi Pattern'leri Kullandık?

| Desen (Pattern) | Uygulandığı Bileşen | Sistemdeki İşlevi |
| :--- | :--- | :--- |
| **Compile-Time Middleware** | `LoggingMiddleware`, `PerformanceMiddleware` | Runtime overhead olmadan her komut ve sorgunun başlangıç, bitiş, süre ve hata anlarını yakalar; PII maskelemesi uygular. |
| **POCO CQRS Pattern** | Commands, Queries, Handlers | Uygulama katmanının herhangi bir framework arayüzüne bağımlı olmadan saf domain kurallarına odaklanmasını sağlar. |
| **Convention over Configuration** | Wolverine Handler Keşfi | Belirli bir arayüz implementasyonu yerine sınıf adı (`*Handler`) ve metot adına (`Handle`) bakarak otomatik bağlama yapar. |
| **Transactional Outbox Pattern** | `ProcessOutboxMessagesJob` & EF Core Interceptor | Veritabanı işlemiyle RabbitMQ event yayını arasındaki tutarlılığı garanti altına alır. |
| **Linked Token Timeout Pattern** | `WaitAsync(linkedCts.Token)` | Asenkron kuyruk yayınında ağ kilitlenmelerine karşı 10 saniyelik zaman aşımı uygulayarak DB transaction kilitlerini serbest bırakır. |
| **Separation of Concerns (SoC)** | `AddApplicationMessaging` & `AddMessagingTransport` | Mesajlaşmanın mantıksal (iş mantığı/middleware) yapılandırmasını Application katmanına; fiziksel (RabbitMQ broker) yapılandırmasını Infrastructure katmanına ayırır. |

---

## 4. Yapılan Geliştirme Nasıl Test Edilir?

### A. Birim ve Mimari Testler (Unit & Architecture Tests)
Birim testler Wolverine middleware'lerinin ve mimari kuralların sağlamlığını doğrular:
```bash
# Mimari testler (Handler'ların public olması, CQRS ayrımı)
dotnet test --filter "FullyQualifiedName~ArchitectureTests"

# Middleware birim testleri (Logging ve Performance)
dotnet test --filter "FullyQualifiedName~MiddlewareTests"
```

### B. Wolverine Pipeline Entegrasyon Testleri (Pipeline Integration Tests)
Wolverine middleware'lerinin gerçek DI konteyneri içinde çalıştığını ve logların doğru formatta düştüğünü doğrulamak için `CatalogMessagingPipelineIntegrationTests` ve `IdentityMessagingPipelineIntegrationTests` sınıfları yazılmıştır:
```bash
# Catalog Pipeline Entegrasyon Testleri
dotnet test --filter "FullyQualifiedName~CatalogMessagingPipelineIntegrationTests"

# Identity Pipeline Entegrasyon Testleri
dotnet test --filter "FullyQualifiedName~IdentityMessagingPipelineIntegrationTests"
```

**Test Senaryoları:**
1. **Normal Akış:** Komut `IMessageBus.InvokeAsync` ile çağrıldığında `Handling command ...` ve `Handled command ... in ...ms` loglarının Information seviyesinde yazıldığı doğrulanır.
2. **Hata Akışı:** Komut istisna fırlattığında `OnException` metodunun `Error handling command ...` logunu Error seviyesinde yazdığı ve hatayı stack trace bozulmadan fırlattığı doğrulanır.
3. **PII Maskeleme:** Şifre veya gizli anahtar içeren komutlarda (`Password`, `Token`) log metninde açık metin yerine `***MASKED***` yazıldığı doğrulanır.
4. **Performans Eşiği:** 500ms'yi aşan yavaş komutlarda `Long Running Request: ... took ...ms` uyarısının Warning seviyesinde üretildiği doğrulanır.

### C. End-to-End Gateway Testleri (E2E Verification)
Tüm servisler ve API Gateway ayağa kaldırılarak Python otomasyon suit'i çalıştırılır:
```bash
python3 e2e_tests/run_all.py
```
**Beklenen Çıktı:**
```text
================================================================================
ALL TEST SUITES PASSED!
================================================================================
Total Suites : 3
Passed       : 3
Failed       : 0
Total Tests  : 126
Passed       : 126
Failed       : 0
```
