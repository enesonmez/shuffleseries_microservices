# Catalog & Identity Bounded Contexts - Güvenlik, Asimetri ve Performans Sertleştirmesi (Hardening)

Bu dokümanda, `ShuffleSeries.Catalog` ve `ShuffleSeries.Identity` mikroservisleri genelinde gerçekleştirilen kapsamlı mimari asimetri giderme, OWASP güvenlik sertleştirmeleri, veritabanı indeks optimizasyonları ve 1-Class-1-File refactoring adımları özetlenmektedir.

---

## 1. Ne Yaptık?

1. **Identity Domain Kapsülleme & Cascade Soft-Delete:**
   - `UserRole`, `UserPermission`, `RolePermission`, `RefreshToken` ve `UserLogin` varlıklarının yapıcı metotları (constructors) `internal` yapılarak yalnızca Aggregate Root (`User` / `Role`) üzerinden yaratılmaları güvence altına alındı.
   - `User.DeleteAccount` metodunda ilişkili tüm `_userLogins` kayıtlarının `SoftDelete` edilmesi ve aktif tüm `RefreshToken`'ların deterministik UTC zaman damgasıyla geçersiz kılınması sağlandı.
   - `RefreshToken.IsActiveAt(DateTime utcNow)` ile `DateTime.UtcNow` sızıntısı giderildi ve `TimeProvider` standartlaştırıldı.

2. **EF Core Kısmi Tekil İndeksler (Partial Unique Indexes) & Migration:**
   - `UserLogin` (`Provider`, `ProviderKey`), `Role` (`NormalizedName`) ve `Permission` (`Code`) üzerindeki tekil indekslere `.HasFilter("\"IsDeleted\" = false")` filtresi eklendi; böylece soft-deleted kayıtların gelecekteki yeni hesap/rol oluşturmalarını engellemesi önlendi.
   - `Users` tablosundaki düşük kardinaliteli `IsDeleted` tekil B-Tree indeksi kaldırılarak gereksiz indeks yazma maliyeti sıfırlandı.
   - `HardenIdentitySoftDeleteIndexes` EF Core migration'ı üretildi.

3. **Sosyal Kimlik Doğrulama Güvenliği (Social Auth & JWKS):**
   - `BaseJwtSocialAuthProvider` içerisine `IHttpClientFactory` ve `TimeProvider` enjekte edildi; `SemaphoreSlim` kaynak sızıntısına karşı `IDisposable` uygulandı.
   - **Pre-Account Takeover Koruması:** Harici kimlik doğrulayıcılarda (Google & Apple) `email` claim'i varsa, `email_verified == "true"` doğrulaması zorunlu kılındı.
   - `ClientId` boş veya tanımsız olduğunda sessizce doğrulamanın atlanması (fail-open) engellendi; fail-closed ilkesiyle geçersiz sayıldı.
   - `ISocialAuthProvider` ve `IExternalAuthService` bağımlılıkları `AddSingleton` olarak tescil edilerek 12 saatlik JWKS bellek önbelleğinin request başına sıfırlanması önlendi.

4. **Kullanıcı Girdisi, X-Forwarded-For & DoS Savunması:**
   - `X-Forwarded-For` başlığından okunan IP adresleri `System.Net.IPAddress.TryParse` ile sanitize edildi; geçersiz spoofed IP'lerin kabul edilmesi engellendi.
   - FluentValidation seviyesinde `IpAddress` (IPv6 uyumlu maks 45 karakter) ve `Password` (PBKDF2 CPU tükenmesini önlemek amacıyla maks 128 karakter) tavan limitleri uygulandı.

5. **Token İptali (Revoke) & Misafir Dönüştürme (Convert Guest):**
   - Misafir hesaptan kalıcı hesaba geçişte (`/api/auth/convert-guest`) mevcut misafir token'ının `jti` değeri `ITokenBlacklistService` üzerinden 60 dakika boyunca Redis kara listesine alındı; yeni kalıcı token çifti üretildi.
   - `/api/auth/revoke` çağrılarında üçüncü taraf token'larının keyfi iptal edilmesini önlemek amacıyla JWT imza doğrulaması ve `sub == CallerUserId` eşleşmesi teyit edildi.

6. **Catalog Doğrulayıcı & Veritabanı Uzunluk Uyumu:**
   - `MustBeHttpUrl()` ve `MustBeCustomAppScheme()` ile Stored XSS ve tehlikeli protokol (`javascript:`, `data:`, `file:`, `blob:`) enjeksiyonları engellendi.
   - `Overview` (2000), `PosterUrl` (500), `BackdropUrl` (500), `Director` (150), `Tagline` (500), `Title` (250), `VibeTags` (500) limitleri veritabanı kolon uzunluklarıyla tam $1:1$ eşitlendi.
   - `Catalog.Infrastructure` içerisindeki kullanılmayan `services.AddMemoryCache();` kaydı temizlendi; `Mood.cs` içindeki `[GeneratedRegex]` desenine ReDoS koruması olarak `matchTimeoutMilliseconds: 250` atandı.

7. **1-Class-1-File Refactoring (Requests & Responses):**
   - `Identity.Api` altındaki `AuthRequests.cs` bundle dosyası 7 müstakil DTO dosyasına bölündü (`RegisterRequest.cs`, `LoginRequest.cs`, `RefreshTokenRequest.cs`, `RevokeTokenRequest.cs`, `SocialLoginRequest.cs`, `ConvertGuestRequest.cs`, `ChangePasswordRequest.cs`).
   - `Catalog.Api` altındaki 7 bundle dosya (`MovieRequests`, `SeriesRequests`, `EpisodeRequests`, `GenreRequests`, `MoodRequests`, `PlatformRequests`, `MediaAssociationRequests`) tamamen ayrılarak müstakil `public sealed record` dosyaları haline getirildi.
   - `TokenResponse` sınıfı `Application.Common.Models` altından `Features.Auth.Responses` klasörüne taşınarak mimari standartla uyumlu hale getirildi.

8. **Mimari Test & SUT Aynalama Paritesi:**
   - `MicroserviceArchitectureTestBase` sınıfına:
     - Tüm somut Domain entity'lerinin `AggregateRootTypes` veya `ChildEntityTypes` içinde tescilli olmasını zorunlu kılan test eklendi.
     - Tüm API Request ve Application Response DTO'larının `public sealed` olmasını zorunlu kılan test eklendi.
   - `CatalogArchitectureTests.cs` içerisindeki `ChildEntityTypes` listesine `MoodTranslation` ve `GenreTranslation` eklendi.
   - `IdentityDbSeederTests.cs` dosyası `Persistence/` altına, `LocalizationExtensionsTests.cs` dosyası `Common/Extensions/` altına taşınarak 1:1 SUT dizin aynalaması sağlandı.
   - `JwtTokenServiceTests` ve `PurgeExpiredRefreshTokensJobTests` unit testleri yazıldı.

---

## 2. Neden Yaptık? (Mimari ve Güvenlik Gerekçeleri)

- **Domain İzolasyonu ve Invariant Koruması:** `UserRole` veya `RefreshToken` nesnelerinin dış katmanlar tarafından kontrolsüz biçimde `new` anahtar kelimesiyle üretilmesi, iş kurallarını (örn. refresh token rotasyon zincirleme bütünlüğü) baypas edebilirdi. Yapıcıların `internal` yapılması invariant'ları aggregate sınırları içinde tutar.
- **Pre-Account Takeover (OWASP API2):** E-posta doğrulaması yapılmamış harici hesapların doğrudan sistemdeki bir kullanıcıyla eşleştirilmesi, saldırganın kurbanın e-postasıyla sahte Google/Apple hesabı açarak hesabı ele geçirmesine (Account Takeover) yol açar.
- **Fail-Closed Güvenlik Felsefesi:** Konfigürasyon eksikliğinde sistemlerin varsayılan olarak "izin ver" yerine "erişimi reddet" tutumu sergilemesi zorunludur.
- **Cache Stampede ve Bellek Tasarrufu:** 12 saatlik JWKS anahtar önbelleğinin her HTTP isteğinde yeniden yaratılan `Transient`/`Scoped` servislerde yaşaması bellek ve dış ağ maliyetini katlar; bu yüzden singleton yaşam döngüsü seçildi.
- **Single Responsibility & 1-Class-1-File:** Kod tabanının okunabilirliği, git çatışmalarının (merge conflicts) asgariye indirilmesi ve mimari kuralların otomatik denetlenebilmesi için her sınıf/kayıt kendi dosyasında yaşamalıdır.

---

## 3. Hangi Pattern'leri Kullandık?

- **Aggregate Pattern (DDD):** Varlıklar arasındaki ilişkiler doğrudan alt nesneler üzerinden değil, Aggregate Root üzerinden yönetilir.
- **Cache-Aside with Double-Check Locking:** JWKS public key'leri çekilirken `SemaphoreSlim` ile eşzamanlı isteklerde ağ darboğazını engelleyen kilitli okuma işletildi.
- **Fail-Closed Pattern:** Güvenlik parametresi doğrulanmadığında işlem sessizce başarılı olmak yerine derhal iptal edilir.
- **Token Blacklisting (Distributed Revocation):** Kullanıcı şifre değiştirdiğinde, hesap dönüştürdüğünde veya çıkış yaptığında token'ın geçerlilik süresi dolana kadar Redis üzerinde O(1) blacklist tutulur.
- **Architecture as Code (NetArchTest):** Mimari kurallar ve klasör simetrileri otomatik testlerle CI/CD hattında doğrulanır.

---

## 4. Nasıl Test Edilir & Test Senaryoları

### Terminal Doğrulaması

```bash
# 1. Kod formatlama kontrolü
dotnet format ShuffleSeries.sln --verify-no-changes

# 2. Çözüm derleme kontrolü (0 uyarı, 0 hata)
dotnet build ShuffleSeries.sln

# 3. Tüm birim, entegrasyon ve mimari testleri çalıştırma (731 test)
dotnet test ShuffleSeries.sln

# 4. Gateway E2E test suitelerini çalıştırma (203 test)
python3 e2e_tests/run_all.py
```

### Öne Çıkan Test Senaryoları

1. **Pre-Account Takeover Testi:** `email_verified: false` içeren bir Google veya Apple idToken sunulduğunda sağlayıcının `null` döndürmesi ve isteğin reddedilmesi (`SocialAuthProviderTests`).
2. **Cross-User Blacklist Koruması:** `A` kullanıcısı `B` kullanıcısına ait bir refresh token'ı revoke etmeye çalıştığında `jti` bilgisinin filtrelenmesi ve sadece token sahibinin oturumunun hedeflenmesi (`JwtTokenServiceTests`).
3. **Misafir Dönüştürme Token Blacklist:** `/api/auth/convert-guest` ile kalıcı olan kullanıcının eski misafir token'ıyla tekrar istek atması durumunda 401 `AUTH_TOKEN_REVOKED` alınması (`test_identity_api_gateway.py`).
4. **URL & XSS Savunması:** `javascript:alert(1)` veya `data:...` şemalı görsel bağlantısı gönderildiğinde 400 Bad Request alınması (`CreateMovieCommandValidatorTests`).
5. **Mimari DTO Tescili:** Tüm request/response tiplerinin `public sealed` ve tüm somut entity'lerin test tabanında tescilli olduğunun otomatik teyit edilmesi (`CatalogArchitectureTests`, `IdentityArchitectureTests`).
