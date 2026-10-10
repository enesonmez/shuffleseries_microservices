# Task 2.5: Identity Sıkılaştırma (Hardening), ConvertFromGuest Mimarisi ve Üretim Hazırlığı

## Ne Yaptık?
Bu görev kapsamında, ShuffleSeries mikroservis ekosisteminin Identity ve API Gateway altyapısı üretim ortamı gereksinimlerine ve katı güvenlik/performans standartlarına göre sıkılaştırıldı (hardening). Kullanıcı onboarding deneyimi sektör standardı **ConvertFromGuest (In-Place Elevation)** mimarisine geçirilerek gereksiz veri taşıma (Data Migration) karmaşıklığı ortadan kaldırıldı; eksik güvenlik denetimleri ve bellek optimizasyonları tamamlandı.

Gerçekleştirilen temel bileşenler ve geliştirmeler:

1. **MergeGuestAccount Mimarisinin Kaldırılması ve In-Place Elevation (ConvertFromGuest):**
   - Eski `MergeGuestAccount` yapısı, `UserMergedDomainEvent`, `UserMergedEvent` ve ilgili özel istisnalar (`CannotMergeIntoDifferentUserException`, `GuestUserNotFoundException`, `TargetUserNotFoundException`) tamamen temizlendi.
   - Tüm roadmap milestone'larındaki (Milestone 6, 7, 8, 9) gereksiz `UserMergedEvent` consumer bağımlılıkları arındırıldı.
   - `POST /api/auth/convert-guest` endpoint'i (`ConvertGuestCommand`, `ConvertGuestCommandValidator`, `ConvertGuestCommandHandler`) hayata geçirildi.
   - Misafir oturumu mevcut `UserId` korunarak yerinde kalıcı hesaba dönüştürüldü; `Email` ve PBKDF2 hashlenmiş şifre atandı, `IsGuest = false` ve `Status = UserStatus.Active` yapıldı, `Guest` rolü `Standard` ile değiştirildi ve `UserRegisteredDomainEvent(Id, Email, IsGuest: false)` tetiklendi.

2. **Parola Güncelleme Endpoint'i (`POST /api/auth/change-password`):**
   - Yetkilendirilmiş kullanıcının parolasını güvenle güncelleyebileceği `POST /api/auth/change-password` endpoint'i geliştirildi.
   - Mevcut parola doğrulaması (`VerifyPassword`), yeni parola güçlü parola kuralları (büyük/küçük harf, rakam, minimum 8 karakter) ve mevcut parolayla aynı olmama kuralı FluentValidation ile garantiye alındı.
   - Şifre güncellendiğinde tüm aktif refresh token'lar iptal edildi (`RevokeAllRefreshTokens()`), kullanıcının güvenlik damgası yenilendi (`TouchSecurityStamp()`) ve Redis üzerinde 1 saatlik kullanıcı bazlı kara liste (`BlacklistUserTokensAsync`) tetiklenerek eski oturumların anında düşürülmesi sağlandı.

3. **Zamanlama Saldırısı Koruması (Timing Attack Defense / Constant-Time Dummy Hash):**
   - `LoginCommandHandler` içinde kullanıcı sistemde bulunamadığında veya inaktif olduğunda hemen çıkmak yerine sabit bir sahte hash (`DummyPasswordHash`) üzerinde `VerifyPassword` işlemi koşturuldu.
   - Böylece e-postanın veritabanında var olup olmadığından bağımsız olarak tüm başarısız giriş denemeleri eşit sürede (~80ms PBKDF2 doğrulama süresi) yanıt verir hale getirildi; saldırganların süre ölçümüyle kullanıcı listesi çıkarması (User Enumeration) engellendi.

4. **Sosyal Girişlerde Pre-Account Takeover Koruması (`email_verified` Denetimi):**
   - `BaseJwtSocialAuthProvider` abstract taban sınıfına katı `email_verified` claim denetimi eklendi.
   - Harici sağlayıcıdan (Google, Apple vb.) gelen ID Token'da `email_verified == "true"` doğrulanmadıkça kimlik doğrulama reddedildi; doğrulanmamış üçüncü parti e-postalarla hesap ele geçirme (Pre-Account Takeover) riski kapatıldı.

5. **API Gateway Seviyesinde İstemci IP Çözümlemesi (`UseForwardedHeaders`):**
   - API Gateway `Program.cs` içine ASP.NET Core `ForwardedHeadersOptions` entegre edildi.
   - `ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto` bayrakları yapılandırıldı; Docker/Kubernetes ağındaki ters vekil sunucuların IP blokları için `KnownIPNetworks.Clear()` ve `KnownProxies.Clear()` ayarlanarak `app.UseForwardedHeaders()` middleware hattının en başına yerleştirildi.
   - Bu sayede downstream mikroservislere ve Rate Limiter'a gerçek istemci IP'si aktarılması sağlandı.

6. **Bellek & Veritabanı Optimizasyonu ve Süresi Dolmuş Token Temizliği (`ExecuteDeleteAsync`):**
   - `UserRepository.GetByIdWithDetailsAsync` metodu üzerinden gereksiz `.Include(u => u.RefreshTokens)` ve `.Include(u => u.UserLogins)` kaldırıldı; `/api/auth/me` sorgularının yalnızca rolleri ve izinleri çekmesi sağlandı.
   - Quartz tabanlı `PurgeExpiredRefreshTokensJob` arka plan işi oluşturuldu; ChangeTracker'a binlerce varlık yüklemeden doğrudan SQL `DELETE` üreten EF Core `ExecuteDeleteAsync()` ile 30 günden eski süresi dolmuş veya iptal edilmiş token'ların periyodik olarak temizlenmesi sağlandı.
   - `DependencyInjection.cs` içinde her 24 saatte bir tetiklenecek şekilde zamanlandı.

7. **Kapsamlı Test Paketi ve Doğrulama:**
   - Yeni unit testler (`ConvertGuestCommandHandlerTests`, `ChangePasswordCommandHandlerTests`, `IdentityDomainExceptionTests`) eklendi.
   - Testcontainers + PostgreSQL tabanlı gerçek entegrasyon testlerine `ConvertGuest`, `ChangePassword` ve `PurgeExpiredRefreshTokensJob` senaryoları eklendi.
   - Çözüm genelindeki tüm 367 test %100 başarıyla geçti ve `dotnet format --verify-no-changes` ile sıfır kod stili uyarısı doğrulandı.

---

## Neden Yaptık? (Mimari Kararlar)

### 1. Neden `MergeGuestAccount` Yerine `ConvertFromGuest` (In-Place Elevation) Seçtik?
- **Problem (Veri Taşıma ve Yarış Durumu Kabusu):**
  Eski modelde, misafir oturumu kalıcı hesaba bağlanırken ayrı bir hedef kullanıcı (`targetUserId`) açılıyor veya mevcut kullanıcı ile birleştiriliyordu (`merge-guest`). Bu yaklaşım dağıtık mikroservis mimarisinde devasa bir karmaşıklık doğuruyordu:
  - Downstream mikroservislerin (`TicketEconomy`, `UserLibrary`, `History & Analytics`, `Profile`) tümünde `UserMergedEvent` dinleyen özel consumer'lar yazılması gerekiyordu.
  - Bu servislerde `UPDATE tickets SET user_id = target_id WHERE user_id = guest_id` gibi asenkron veri göçü (data migration) koşturulmak zorundaydı.
  - Kullanıcı mobil cihazda aynı anda film swipe ederken veya bilet harcarken arkadan merge event'i geldiğinde veri tutarsızlığı, çift bilet kullanımı ve distributed race condition'lar kaçınılmazdı.
- **Çözüm (In-Place Elevation):**
  Sektör standardı oyun ve eğlence platformlarında (Spotify, Duolingo, Netflix) uygulandığı gibi: Misafirin `UserId` kimliği **kesinlikle değiştirilmez**. Misafirin var olan satırına doğrudan `Email` ve `PasswordHash` atanır, rolü `Standard` yapılır.
  - Downstream servislerde tek bir satır dahi veri taşınmasına gerek kalmaz.
  - Kullanıcının kazandığı streak, harcadığı biletler, izleme geçmişi ve favori listesi yerinde kesintisiz korunur.
  - `UserMergedEvent` ve ilgili tüm downstream consumer yükü sıfırlanır.

### 2. Neden `LoginCommandHandler` İçinde Sabit Zamanlı Sahte Hash (DummyHash) Kullandık?
- **Timing Attack (Zamanlama Saldırısı) Nedir?:**
  Kriptografik parola hashleme algoritmaları (PBKDF2, Argon2, BCrypt), brute-force saldırılarını yavaşlatmak için yüksek iterasyon sayısıyla (örneğin 100.000 iterasyon) CPU üzerinde ~80 milisaniye sürer.
  Eğer sistemde kayıtlı olmayan bir e-posta girildiğinde veritabanından kullanıcı dönmediği için hemen `401 Unauthorized` dönülürse istek ~1-2 milisaniye sürer. Kayıtlı bir kullanıcı için yanlış şifre girildiğinde ise hash doğrulama çalıştığı için istek ~82 milisaniye sürer.
- Saldırganlar ağ isteklerinin yanıt sürelerindeki bu ~80ms farkı analiz ederek sistemde hangi e-postaların kayıtlı olduğunu kolayca keşfedebilir (User Enumeration).
- **Çözüm:** Kullanıcı veritabanında bulunamadığında dahi önceden üretilmiş sabit bir sahte hash üzerinde `VerifyPassword` koşturularak her iki senaryonun da aynı CPU maliyeti ve süreyle yanıt vermesi sağlandı.

### 3. Neden `BaseJwtSocialAuthProvider` İçinde `email_verified` Denetimi Zorunlu Kılındı?
- **Pre-Account Takeover Açığı:**
  OAuth sağlayıcılarında bazı kullanıcılar e-posta adreslerini doğrulamadan hesap açabilir (veya saldırgan kurbanın e-postasını kendi hesabına ekleyip henüz doğrulamamış olabilir).
  Eğer kimlik servisi salt `email` claim'ine bakarak hesabı eşlerse, saldırgan henüz doğrulanmamış bir token ile sisteme giriş yaparak meşru kullanıcının hesabını ele geçirebilir.
- **Çözüm:** `email_verified == "true"` şartı zorunlu kılınarak yalnızca sağlayıcı tarafından sahipliği doğrulanmış e-postaların sisteme kabul edilmesi garanti altına alındı.

### 4. Neden API Gateway Seviyesinde `UseForwardedHeaders` Kullanıldı?
- API Gateway, Docker container veya Kubernetes Ingress arkasında çalıştığında istemcinin doğrudan TCP soket IP'sini değil, bir önceki ters vekil sunucunun IP'sini görür.
- `UseForwardedHeaders` yapılandırması olmadan `HttpContext.Connection.RemoteIpAddress` daima `172.18.0.1` gibi iç ağ IP'si döner.
- Bu durum hem Rate Limiting'in tüm kullanıcıları tek bir IP sanarak yanlışlıkla engellemesine, hem de şüpheli oturum açma kayıtlarında gerçek istemci IP'sinin kaybolmasına yol açar.

### 5. Neden Refresh Token Temizliğinde `ExecuteDeleteAsync` Tercih Edildi?
- `DbContext.RemoveRange` ChangeTracker mekanizmasını kullanır; binlerce süresi dolmuş token entity'sini belleğe yükler ve her biri için ayrı SQL ifadeleri üretir.
- `ExecuteDeleteAsync()`, varlıkları belleğe hiç almadan doğrudan PostgreSQL üzerinde tek bir optimize `DELETE FROM "RefreshTokens" WHERE ...` sorgusu koşturur. Bu sayede bellek ayırmaları (heap allocation) sıfırlanır ve GC duraklamaları önlenir.

---

## Hangi Pattern'leri Kullandık?

| Tasarım Deseni / Mimari Prensip | Sistemdeki İşlevi |
| :--- | :--- |
| **In-Place Elevation** | Misafir oturumunun aynı `UserId` kimliği ile kalıcı hesaba dönüştürülmesi; dağıtık veri göçünü sıfırlama. |
| **Constant-Time Verification** | Başarısız login denemelerinde sahte PBKDF2 hash'i koşturularak zamanlama saldırılarının (Timing Attack) önlenmesi. |
| **Strategy Pattern** | `ISocialAuthProvider` ve `BaseJwtSocialAuthProvider` ile yeni sağlayıcıların OCP prensibiyle kolayca eklenebilmesi. |
| **Bulk SQL Execution** | EF Core `ExecuteDeleteAsync()` ile ChangeTracker'ı devre dışı bırakarak arka plan temizliklerinde sıfır bellek tahsisatı. |
| **Transactional Outbox & ACL** | Dönüşüm sonrası `UserRegisteredDomainEvent` tetiklenerek ortak `UserRegisteredEvent` sözleşmesiyle asenkron duyurulması. |
| **Edge Perimeter Defense** | API Gateway seviyesinde `ForwardedHeaders` ve Redis Blacklist denetimi ile yetkisiz isteklerin iç ağa girmeden kesilmesi. |

---

## Yapılan Geliştirme Nasıl Test Edilir?

### 1. Otomatik Testlerin Çalıştırılması

Tüm çözümdeki test paketlerini çalıştırmak için:
```bash
dotnet test ShuffleSeries.sln
```
Sonuç: **367 testin tamamı (Unit, Architecture, Integration) 0 hata ile geçer.**

### 2. HTTP Dosyası ile Uçtan Uca Manuel Doğrulama (`ShuffleSeries.Identity.Api.http`)

#### Adım 1: Misafir Oturumu Başlatma
```http
POST http://localhost:5011/api/auth/guest
Content-Type: application/json
```
Dönen yanıttan `accessToken` ve `userId` değerini alın.

#### Adım 2: Misafir Hesabını Kalıcı Hesaba Yükseltme (Convert-Guest)
```http
POST http://localhost:5011/api/auth/convert-guest
Authorization: Bearer <GUEST_ACCESS_TOKEN>
Content-Type: application/json

{
  "email": "promoted.user@shuffleseries.com",
  "password": "Password123!"
}
```
* **Beklenen Sonuç:** `200 OK`. Dönen yanıttaki `userId` değeri misafir ile birebir aynıdır, `isGuest: false` döner, rollere `Standard` atanır.
* PostgreSQL `OutboxMessages` tablosunda `UserRegisteredDomainEvent` mesajı oluşur.

#### Adım 3: Parola Güncelleme (Change Password)
```http
POST http://localhost:5011/api/auth/change-password
Authorization: Bearer <NEW_USER_ACCESS_TOKEN>
Content-Type: application/json

{
  "currentPassword": "Password123!",
  "newPassword": "UpdatedPassword456!"
}
```
* **Beklenen Sonuç:** `204 No Content`. Eski şifreyle giriş yapılamaz (`401`), yeni şifreyle giriş başarılı olur (`200 OK`).

---

## Test Senaryoları (Test Cases)

| Test Türü | Test Sınıfı / Senaryo | Beklenen Davranış |
| :--- | :--- | :--- |
| **Unit Test** | `ConvertGuestCommandHandlerTests.Handle_WhenUserNotFound` | `UserNotFoundException` fırlatılmalıdır. |
| **Unit Test** | `ConvertGuestCommandHandlerTests.Handle_WhenUserIsNotGuest` | Kullanıcı zaten kalıcıysa `UserAlreadyRegisteredException` fırlatılmalıdır. |
| **Unit Test** | `ConvertGuestCommandHandlerTests.Handle_WhenEmailAlreadyInUse` | E-posta çakışmasında `EmailAlreadyInUseException` fırlatılmalıdır. |
| **Unit Test** | `ConvertGuestCommandHandlerTests.Handle_WhenValidGuest` | Kullanıcı kalıcıya yükseltilmeli, `Standard` rol verilmeli ve yeni token seti dönmelidir. |
| **Unit Test** | `ChangePasswordCommandHandlerTests.Handle_WhenCurrentPasswordIncorrect` | Mevcut şifre hatalıysa `InvalidCurrentPasswordException` fırlatılmalıdır. |
| **Unit Test** | `ChangePasswordCommandHandlerTests.Handle_WhenCurrentPasswordCorrect` | Şifre güncellenmeli, refresh token'lar iptal edilmeli ve Redis blacklist çağrılmalıdır. |
| **Integration Test** | `AuthEndpointsIntegrationTests.ConvertGuest_WithValidPayload` | Gerçek PostgreSQL'de misafir hesabı güncellenmeli ve Outbox'a `UserRegisteredDomainEvent` yazılmalıdır. |
| **Integration Test** | `AuthEndpointsIntegrationTests.ChangePassword_WithValidCredentials` | Parola değiştikten sonra eski parola ile login 401 dönmeli, yeni parola ile login 200 OK dönmelidir. |
| **Integration Test** | `AuthEndpointsIntegrationTests.PurgeExpiredRefreshTokensJob` | Testcontainers PostgreSQL üzerinde 30 günden eski süresi dolmuş/iptal edilmiş token'lar `ExecuteDeleteAsync` ile silinmeli, aktif token'lar korunmalıdır. |
