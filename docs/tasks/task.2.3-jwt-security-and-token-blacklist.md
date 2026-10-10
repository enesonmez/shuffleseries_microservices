# Task 2.3: JWT Güvenliği & Token Blacklist (Redis)

## Ne Yaptık?
Bu görev kapsamında, mikroservis mimarisinde durumsuz (stateless) çalışan JWT (JSON Web Token) mekanizmasının doğasında bulunan "verilen token süresi dolana kadar iptal edilemez" güvenlik açığını ortadan kaldıran, endüstri standardı **Redis Tabanlı Merkezi Token Blacklist (Kara Liste)** altyapısı tasarlandı ve uçtan uca uygulandı.

Gerçekleştirilen temel bileşenler ve geliştirmeler:
1. **JWT Güvenlik Parametrelerinin Sıkılaştırılması (OWASP API4):**
   * Token yapısına benzersiz belirteç kimliği (`jti`), üretilme zamanı (`iat`), geçerlilik başlangıcı (`nbf`) ve oturum güvenlik mührü (`security_stamp`) claim'leri eklendi.
   * Varsayılan 5 dakikalık ASP.NET Core saat toleransı kaldırılarak **`ClockSkew = TimeSpan.Zero`** zorunlu kılındı. Böylece süresi biten veya iptal edilen token'lar anında geçersiz sayılır.

2. **Merkezi Token Blacklist Servisi (`ITokenBlacklistService` & `RedisTokenBlacklistService`):**
   * `ShuffleSeries.Shared.Core.Application` katmanında `ITokenBlacklistService` arayüzü tanımlandı.
   * `ShuffleSeries.Shared.Core.Infrastructure` katmanında `StackExchange.Redis` kullanılarak yüksek performanslı `RedisTokenBlacklistService` gerçeklendi.
   * İki kademeli iptal mimarisi kurgulandı:
     * **Tekil Token İptali (`blacklist:token:{jti}`):** Kullanıcı çıkış yaptığında (`/api/auth/revoke`) mevcut erişim belirtecinin kalan ömrü kadar TTL ile Redis'e yazılır.
     * **Kullanıcı Bazlı Toplu İptal (`blacklist:user:{userId}`):** Hesap silindiğinde (`/api/auth/account`) veya Token Reuse (yeniden kullanım saldırısı) tespit edildiğinde, kullanıcının o ana kadar üretilmiş tüm token'larını geçersiz kılan Unix zaman damgası TTL ile Redis'e kaydedilir.

3. **API Gateway Seviyesinde Sınır Denetimi (Edge Enforcement):**
   * `ShuffleSeries.Shared.Core.Web` katmanındaki `AddSharedJwtAuthentication` genişletilerek `JwtBearerEvents.OnTokenValidated` hook'u içine Redis kara liste kontrolü entegre edildi.
   * Kara listedeki bir token ile istek geldiğinde `context.Fail()` tetiklenerek istek daha alt mikroservislere (downstream) hiç ulaşmadan API Gateway sınırında durdurulur ve RFC 7807 uyumlu ProblemDetails ile `401 Unauthorized` (`AUTH_TOKEN_REVOKED`) yanıtı döndürülür.

4. **Kullanıcı Yaşam Döngüsü ve Oturum Entegrasyonu:**
   * `RevokeTokenCommandHandler`: Çıkış isteğinde refresh token DB'de iptal edilirken, gönderilen access token'ın `jti` değeri hesaplanıp kalan süresiyle Redis kara listesine eklenir.
   * `DeleteAccountCommandHandler`: Hesap soft-delete yapıldığında ve refresh token'lar kapatıldığında `BlacklistUserTokensAsync` çağrılarak tüm aktif erişim token'ları iptal edilir.
   * `RefreshTokenCommandHandler`: İptal edilmiş bir refresh token yeniden kullanılmaya çalışıldığında (OWASP API4 Reuse Detection), kullanıcının tüm oturumları kapatılır ve Redis'te anlık Unix zaman damgasıyla toplu iptal gerçekleştirilir.

5. **HashiCorp Vault & Dağıtık Yapılandırma Entegrasyonu:**
   * Vault'taki `shuffleseries/shared` sır dizinine `Redis:Host`, `Redis:Port` ve `Redis:Password` anahtarları eklendi.
   * Docker Compose üzerinde mikroservislerin Redis'e sorunsuz ve güvenli erişmesi sağlandı.

---

## Neden Yaptık? (Mimari Kararlar)

### 1. Neden Tamamen Durumsuz (Stateless) JWT Yerine Redis Token Blacklist Tercih Ettik?
* **Problem:** Klasik JWT mimarisinde token sunucu tarafında saklanmaz. Kullanıcı "Çıkış Yap" dediğinde veya hesabı silindiğinde istemci tarafındaki token silinir; fakat token network katmanında veya kötü niyetli bir üçüncü parti tarafından kopyalanmışsa, token'ın `exp` süresi dolana kadar yetkili kaynaklara erişmeye devam edebilir.
* **Çözüm (Token Blacklist):** Veritabanına (PostgreSQL) her istekte sorgu atmak distributed mimaride kabul edilemez bir I/O darboğazı (bottleneck) yaratır. Redis In-Memory Key-Value veri deposu, O(1) arama karmaşıklığı ve mikro-saniye (<1ms) gecikme süresi ile idealdir. Tüm geçerli token'ları beyaz listede (whitelist) tutmak yerine yalnızca iptal edilen token'ları (blacklist) TTL süresiyle tutarak Redis bellek ayak izini minimal düzeyde tuttuk.

### 2. Neden İki Kademeli İptal Mimarisi (Two-Tier Revocation) Kurguladık?
* **Tekil Token İptali (`blacklist:token:{jti}`):**
  * Kullanıcı normal çıkış (logout) yaptığında sadece o anki oturumunun access token'ı kara listeye alınır.
  * Anahtar TTL'i token'ın kalan geçerlilik süresi (`exp - now`) kadar verilir. Token zaten süresi bittiğinde geçersiz olacağı için Redis anahtarı TTL bitiminde otomatik olarak temizler (bellek sızıntısı olmaz).
* **Kullanıcı Bazlı Zaman Damgası İptali (`blacklist:user:{userId}`):**
  * Kullanıcı şifresini değiştirdiğinde, hesabı silindiğinde veya bir oturum çalınma atağı (Token Reuse) yaşandığında, o kullanıcının birden fazla cihazdaki onlarca token'ının `jti` değerini tek tek bilmemiz mümkün değildir.
  * Bu durumda Redis'e `blacklist:user:{userId} = 1791559647` (revocation unix timestamp) yazılır.
  * İstek geldiğinde token'ın `iat` (issued-at) değeri ile bu damga kıyaslanır: `token.iat <= user.revokedAt` ise token anında reddedilir. Böylece tek bir Redis yazmasıyla kullanıcının geçmişteki tüm aktif oturumları anında sonlandırılır.

### 3. Neden In-Memory L1 Hibrit Cache Yerine Doğrudan Redis (Single Source of Truth) Tercih Ettik?
* **Aşırı Mühendislikten Kaçınma (KISS Prensibi):** In-Memory `IMemoryCache` (L1) katmanı eklendiğinde, Gateway'in birden fazla pod/instance olarak çalıştığı küme ortamlarında (Kubernetes / Docker Swarm) bir Gateway instance'ında iptal edilen token diğer Gateway instance'larının yerel belleğinde kalır (Cache Invalidation / Desynchronization problemi).
* Bunu çözmek için Redis Pub/Sub ile invalidation broadcast mekanizması kurmak gerekecekti. Yerel ağda Redis sorguları <0.5ms sürdüğünden doğrudan Redis'i "Tek Gerçeklik Kaynağı" (Single Source of Truth) olarak kullanmak, karmaşıklığı sıfıra indirirken yüksek tutarlılık (strong consistency) sağladı.

### 4. Neden Zero Clock Skew (Sıfır Saat Sapması) Zorunlu Kılındı?
* ASP.NET Core `TokenValidationParameters.ClockSkew` varsayılan değeri 5 dakikadır. Bu durum 15 dakikalık bir access token'ın aslında 20 dakika geçerli olmasına ve kullanıcı çıkış yaptıktan sonra token'ın arka planda beklenenden daha uzun süre yaşayabilmesine sebep olur.
* Dağıtık sistemlerimizde NTP (Network Time Protocol) senkronizasyonu sağlandığı için `ClockSkew = TimeSpan.Zero` yapılarak sıfır toleransla sıkı zaman güvenliği uygulandı.

### 5. Neden Denetim API Gateway Sınırında (Edge Enforcement) Yapıldı?
* Bir mikroservis mimarisinde iptal edilmiş bir token'ın API Gateway'i geçerek `ShuffleEngine`, `Catalog` veya `Identity` mikroservislerine kadar gitmesi; gereksiz ağ trafiğine, CPU tüketimine ve iç servis yüküne sebep olur.
* Token kara liste denetimi YARP API Gateway'in `OnTokenValidated` hook'unda yapılarak zararlı veya iptal edilmiş istekler **en dış savunma hattında (Perimeter Defense)** kesilir.

### 6. Neden Fail-Open Stratejisi İle Tasarlandı?
* Redis servisinin geçici olarak ulaşılamadığı (network partition, failover) nadir kriz anlarında meşru kullanıcıların sisteme erişimini tamamen kesip (DDoS etkisi yaratmak yerine) hata loglanarak (`_logger.LogWarning`) geçici olarak erişime izin verilir (Fail-Open for High Availability). Güvenlik ve yüksek erişilebilirlik dengesi sektör standartlarına göre sağlandı.

### 7. Neden Redis Bağlantısı, Sağlık Kontrolü ve Blacklist Ayrı Ayrı (Decoupled) Tanımlandı?
* Tek sorumluluk prensibi (SRP) ve kod okunabilirliği gereğince; Redis bağlantı havuzu (`AddSharedRedis`), hazır bulunuşluk sağlık kontrolü (`AddSharedRedisHealthCheck`) ve iş yeteneği (`AddSharedTokenBlacklist`) tek bir fonksiyonda birbirine gizlenmeyip açık ve bağımsız extension metotları olarak kaydedildi.
* Bu yaklaşım; kodu inceleyen bir mühendisin uygulamanın Redis altyapısını, readiness sağlık kontrolünü ve blacklist servisini nasıl kurduğunu şeffaf bir şekilde görmesini sağlar (`AddSharedRedis -> AddSharedRedisHealthCheck -> AddSharedTokenBlacklist`).
* Redis sağlık kontrolü varsayılan olarak `HealthStatus.Degraded` hata statüsüne sahiptir; böylece Token Blacklist'in fail-open felsefesi korunur ve Redis'teki geçici bir yavaşlıkta orkestratörün (Kubernetes/ALB) pod'ları öldürmesi önlenir.

---

## Hangi Pattern'leri Kullandık?

| Pattern | Projedeki Yeri & İşlevi |
| :--- | :--- |
| **Token Blacklist Pattern** | İptal edilen JWT `jti` değerlerinin kalan token süresi kadar TTL ile Redis'te tutulması. |
| **Edge Security Enforcement** | API Gateway seviyesinde kimlik doğrulama olayları (`OnTokenValidated`) ile downstream trafiğini koruma. |
| **Timestamp-based Revocation** | Kullanıcı seviyesinde toplu belirteç iptali için `userId -> revoked_at` Unix damgası yönetimi. |
| **Fail-Open Resilience Pattern** | Redis kesintilerinde tüm sistemi kilitlemek yerine kontrollü hata toleransı ve audit loglama. |
| **Decoupled DI Registration** | `AddSharedRedis`, `AddSharedRedisHealthCheck` ve `AddSharedTokenBlacklist` ile ayrık ve okunabilir DI mimarisi. |
| **Clean Architecture & DIP** | `ITokenBlacklistService` arayüzünün Application katmanında, Redis uygulamasının Infrastructure katmanında izole edilmesi. |
| **Single Source of Truth (SSOT)** | İptal durumunun paylaşımlı ve dağıtık Redis örneği üzerinden merkezi doğrulanması. |

---

## Yapılan Geliştirme Nasıl Test Edilir?

### 1. Test Piramidi Doğrulaması

1. **Unit Tests (`ShuffleSeries.Shared.Core.Tests`):**
   * `RedisHealthCheckTests`:
     * Redis bağlantısı aktif ve ping yanıt verdiğinde `Healthy` durum, gecikme süresi (`latencyMs`) ve endpoint bilgisinin doğrulanması.
     * Redis bağlantısı koptuğunda `Degraded` durumunun dönülmesi.
     * Ping exception fırlattığında `Degraded` durumu ve hata detayının loglanması.
     * `AddSharedRedisHealthCheck` DI kaydının varsayılan ve özelleştirilmiş parametrelerle doğrulanması.
   * `RedisTokenBlacklistServiceTests`:
     * Geçerli `jti` ve pozitif TTL ile Redis'e formatlanmış anahtarın yazılması.
     * Süresi geçmiş (negatif/sıfır) TTL'lerde Redis yazımının atlanması.
     * Redis bağlantı hatalarında servisin exception fırlatmayıp loglaması (Fail-Safe).
     * `IsTokenBlacklistedAsync` ile var olan ve olmayan anahtarların sorgulanması.
     * `BlacklistUserTokensAsync` ile Unix zaman damgasının TTL ile saklanması.
     * `IsUserBlacklistedAsync` ile damga öncesi token'ların `true`, damga sonrası token'ların `false` dönmesi.
   * `AuthenticationExtensionsTests`:
     * `AddSharedJwtAuthentication` metodunun `ClockSkew = TimeSpan.Zero` yapılandırmasını ve güvenlik hook'larını doğrulayan testler.
2. **Unit Tests (`ShuffleSeries.Identity.Tests`):**
   * `RevokeTokenCommandHandlerTests`:
     * Access token veya `JwtId` gönderildiğinde `ITokenBlacklistService.BlacklistTokenAsync` çağrısının doğrulanması.
   * `DeleteAccountCommandHandlerTests`:
     * Hesap silindiğinde `BlacklistUserTokensAsync` metodunun çağrıldığının doğrulanması.
   * `RefreshTokenCommandHandlerTests`:
     * Token reuse (yeniden kullanım) atağında `BlacklistUserTokensAsync` ile ailenin tüm token'larının kara listeye alındığının doğrulanması.
3. **Architecture Tests (`ShuffleSeries.ArchitectureTests`):**
   * Katman sınırları ve bağımlılık yönlerinin kurallara uygunluğu (30/30 test başarılı).

### 2. Uçtan Uca E2E Doğrulama (Exhaustive Curl Test Suite)

Tüm senaryolar `curl_test_auth_suite.sh` scripti aracılığıyla YARP Gateway (`http://localhost:5001`) üzerinden uçtan uca otomatik test edilmiştir.

```bash
# Tüm suite'i çalıştırma:
bash scratch/curl_test_auth_suite.sh http://localhost:5001
```

#### Test Sonuç Çıktısı:
```text
===========================================================================
🚀 EXHAUSTIVE CURL AUTH TEST SUITE
   Target Base URL: http://localhost:5001
===========================================================================

--- 1. Health Checks ---
[PASS] 1.1 Liveness Probe -> HTTP 200 (Expected: 200)
[PASS] 1.2 Readiness Probe -> HTTP 200 (Expected: 200)

--- 2. Register Flow ---
[PASS] 2.1 Register Standard User (Happy Path) -> HTTP 200 (Expected: 200)
[PASS] 2.2 Register Validation Error: Invalid Email -> HTTP 400 (Expected: 400)
[PASS] 2.3 Register Validation Error: Weak Password -> HTTP 400 (Expected: 400)
[PASS] 2.4 Register Duplicate Email Conflict -> HTTP 409 (Expected: 409)

--- 3. Login Flow ---
[PASS] 3.1 Login With Valid Credentials -> HTTP 200 (Expected: 200)
[PASS] 3.2 Login Invalid Password (401) -> HTTP 401 (Expected: 401)
[PASS] 3.3 Login Non-Existent User (401) -> HTTP 401 (Expected: 401)
[PASS] 3.4 Login Missing Password (400) -> HTTP 400 (Expected: 400)

--- 4. Profile & Authorization (/api/auth/me) ---
[PASS] 4.1 Get Profile With Bearer Token -> HTTP 200 (Expected: 200)
[PASS] 4.2 Get Profile Without Token (401) -> HTTP 401 (Expected: 401)
[PASS] 4.3 Get Profile With Invalid Token (401) -> HTTP 401 (Expected: 401)

--- 5. Guest Session & Data Merge ---
[PASS] 5.1 Create Anonymous Guest Session -> HTTP 200 (Expected: 200)
[PASS] 5.2 Merge Guest Into Authenticated User (204) -> HTTP 204 (Expected: 204)
[PASS] 5.3 Merge Guest IDOR Attack (403 Forbidden) -> HTTP 403 (Expected: 403)

--- 6. Social Login Flow (Extensible Provider Pattern) ---
[PASS] 6.1 Google Social Login With Mock Token (200) -> HTTP 200 (Expected: 200)
[PASS] 6.2 Apple Social Login With Mock Token (200) -> HTTP 200 (Expected: 200)
[PASS] 6.3 Social Login Unsupported Provider (400) -> HTTP 400 (Expected: 400)
[PASS] 6.4 Social Login Missing Token (400) -> HTTP 400 (Expected: 400)

--- 7. Refresh Token Rotation (RTR) ---
[PASS] 7.1 Refresh Token Rotation Success (200) -> HTTP 200 (Expected: 200)
[PASS] 7.2 Token Reuse Attack - Family Revocation (401) -> HTTP 401 (Expected: 401)
[PASS] 7.3 Refresh With Non-Existent Token (401) -> HTTP 401 (Expected: 401)

--- 8. Revoke Token (Logout) ---
[PASS] 8.1 Revoke Active Token (204 No Content) -> HTTP 204 (Expected: 204)
[PASS] 8.2 Revoke Non-Existent Token (404 Not Found) -> HTTP 404 (Expected: 404)

--- 9. Delete Account ---
[PASS] 9.1 Delete Account Without Token (401) -> HTTP 401 (Expected: 401)
[PASS] 9.2 Delete Account With Token (204) -> HTTP 204 (Expected: 204)
[PASS] 9.3 Delete Already Deleted Account Blocked By Token Blacklist (401) -> HTTP 401 (Expected: 401)

--- 10. Redis Token Blacklist Security Enforcement (Task 2.3) ---
[PASS] 10.1.1 Active Access Token Works Before Revoke (200) -> HTTP 200 (Expected: 200)
[PASS] 10.1.2 Revoke Token With AccessToken Included (204) -> HTTP 204 (Expected: 204)
[PASS] 10.1.3 Revoked Access Token Is Blocked by Redis Blacklist (401 AUTH_TOKEN_REVOKED) -> HTTP 401 (Expected: 401)
[PASS] 10.2.1 Delete Account (204) -> HTTP 204 (Expected: 204)
[PASS] 10.2.2 Access Token of Deleted Account Blocked by User Blacklist (401 AUTH_TOKEN_REVOKED) -> HTTP 401 (Expected: 401)
[PASS] 10.3.1 Token Reuse Attack Triggers Family Revocation (401) -> HTTP 401 (Expected: 401)
[PASS] 10.3.2 Newly Issued Token Is Blocked by User Revocation Timestamp (401 AUTH_TOKEN_REVOKED) -> HTTP 401 (Expected: 401)

===========================================================================
📊 TEST RESULTS: Passed: 35 / Total: 35 (100% SUCCESS)
===========================================================================
```
