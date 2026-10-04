# Task 2.1: YARP API Gateway Kurulumu & Güvenlik Yapılandırması

## Ne Yaptık?
Bu görev kapsamında, Milestone 1'de temeli atılan `ShuffleSeries.ApiGateway` projesini YARP (Yet Another Reverse Proxy) altyapısı ile güvence altına aldık. Geliştirmeler şu kısımları kapsamaktadır:

1. **Yerleşik Rate Limiting (Kaba Kuvvet ve DDoS Koruması):**
   * .NET'in `Microsoft.AspNetCore.RateLimiting` middleware'i kullanılarak `AddSharedRateLimiter` extension'ı oluşturuldu.
   * `GlobalLimiter` ile sistem genelinde saniyede 100 istek sınırı (IP başına) getirildi.
   * `StrictLimiter` (saniyede 5 istek) isimli özel politika tanımlandı ve Gateway'de kimlik doğrulama (`auth-route`) ile bilet/shuffle (`shuffle-route`) gibi yüksek yük getiren rotalara bağlandı.

2. **Merkezi JWT Authentication:**
   * Gateway'in JWT token'larını çözümleyebilmesi ve geçerliliğini (Issuer, Audience, Lifetime) kontrol edebilmesi için `AddSharedJwtAuthentication` oluşturuldu.
   * YARP üzerinden geçen isteklerin doğrudan Gateway seviyesinde doğrulanması (Authentication) ve yetkisiz isteklerin iç servislere ulaşmadan (401/403) reddedilmesi sağlandı.

3. **Role & Claims Bazlı YARP Authorization:**
   * Geliştirilen YARP rotalarında (Örn: `shuffle-vip-route`) YARP'ın `AuthorizationPolicy: "RequirePremiumRole"` ayarı kullanılarak sadece Premium yetkiye sahip kullanıcıların bu uç noktayı kullanabilmesi sağlandı.

## Neden Yaptık? (Mimari Kararlar)
* **Neden Gateway Seviyesinde Auth Yaptık?**
  Mikroservis mimarilerinde güvenlik (Security) en dış katmanda başlar (Defense in Depth). Geçersiz veya süresi dolmuş bir token ile gelen isteğin Catalog veya Shuffle servislerine kadar ulaşıp oradaki CPU ve ağ kaynaklarını tüketmesine izin vermek, "Cascade Failure" ve gereksiz yük riskini artırır. Gateway seviyesindeki "Zero Trust" kontrolü, iç servisleri yalıtır.
* **Neden `AspNetCoreRateLimit` gibi 3. Parti Kütüphaneler Yerine Yerleşik Kütüphaneyi Seçtik?**
  .NET 7+ (ve şu anki .NET 10) yerleşik Rate Limiting middleware'i çok daha düşük bellek ayırması (allocation-free) yapar ve performanslıdır. Ayrıca YARP ile doğrudan uyumlu (Out-of-the-box) çalışarak konfigürasyon üzerinden politika eşleştirmesine (`RateLimiterPolicy`) olanak tanır.
* **Neden `FixedWindow` Algoritması?**
  Auth ve Swipe gibi operasyonlarda kısıtlama açık ve net olmalıdır. Token Bucket veya Sliding Window, karmaşık kotalar için iyidir ancak bir saniye içinde 5 şifre denemesi yapan kötü niyetli bir cihazın `FixedWindow` ile saniyesinde katı bir şekilde kesilmesi (anında 429 yanıtı dönülmesi) kaynakları korumak adına en pratik ve öngörülebilir stratejidir.

## Hangi Pattern'leri Kullandık?
* **API Gateway Pattern:** Çoklu mikroservislerin tek bir dış uç noktada toplanması.
* **Edge Security / Defense in Depth:** Tüm doğrulama ve sınırlamaların (Rate Limit, JWT Auth) dış dünyayla temas eden sınırda (Edge) çözümlenmesi.

## Nasıl Test Edilir?
1. **Rate Limiting Doğrulaması:**
   `http://localhost:5001/api/auth/login` endpoint'ine ardışık 5'ten fazla istek atıldığında HTTP 429 (Too Many Requests) hatası alınmalıdır.
2. **Authorization (Yetkilendirme) Doğrulaması:**
   `http://localhost:5001/api/shuffle/vip-weekend` endpoint'ine Authorization Header olmadan veya `"Premium"` rolü içermeyen bir JWT ile istek atıldığında, isteğin Gateway tarafından HTTP 401 veya HTTP 403 ile geri çevrilip, iç servise (`http://shuffle.api:8080`) ulaşmaması doğrulanmalıdır.
3. Architecture ve Unit Test'ler (Milestone test aşamalarında entegre edilecektir).
