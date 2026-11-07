# Task 3.3 — Proje Geneli v3 Güvenlik, Performans ve Yalın Kod Sertleştirmesi (Security, Performance & Lean Hardening)

## 1. Ne Yaptık?
Proje genelinde `/performance-guard`, `/threat-modeling` (STRIDE & OWASP API Top 10) ve `/ponytail` (YAGNI, DRY, BCL) prensipleri ekseninde kapsamlı bir mimari revizyon gerçekleştirildi:
1. **Pre-Account Takeover Savunması:** `UserRepository.GetByEmailWithLoginsAsync` metoduna aktif `RefreshTokens` koleksiyonu dahil edilerek, kurbanın sosyal girişle bağlanması sırasında saldırganın önceden açılmış oturumunun tamamen iptal edilmesi sağlandı. Yeni üretilen erişim token'ının yanlışlıkla kara listeye alınmasını engellemek için `revokedAtCutoff` zaman damgası kurgulandı.
2. **Child Entity Domain Event İzolasyonu:** `Episode` ve `Season` entity'lerinin `base(Guid.Empty)` ile takip edilmesi sebebiyle Outbox'a ve RabbitMQ'ya giden `EpisodeId = Guid.Empty` veri bozulması giderildi. `Episode.GetDomainEvents()` üzerinden gecikmeli event üretimi sağlanarak hem gerçek kimliklerin iletilmesi hem de mükerrer `EpisodeUpdatedDomainEvent` yayınlanması önlendi.
3. **Rate Limiting DoS & Credential Stuffing Savunması:** Gateway üzerindeki `StrictLimiter` politikası, guest token üretilerek aşılamayacak şekilde istemci IP adresine (`$"ip:{RemoteIpAddress}"`) bağlandı.
4. **GDPR / KVKK PII Anonimleştirmesi:** Hesap silme (`DeleteAccount`) işleminde `UserLogin` verileri `Anonymize()` ile temizlendi; geçmişteki tüm refresh token'ların `CreatedByIp` adresleri ChangeTracker'a entity yüklemeden `ExecuteUpdateAsync` ile doğrudan SQL seviyesinde sıfırlandı.
5. **TMDB ID Kompozit Tekil İndeksi:** TMDB API'sinin filmler ve TV dizileri için bağımsız ID dizileri kullanması sebebiyle `MediaConfiguration` üzerindeki global unique indeks `(MediaType, TmdbId)` olarak kompozit hale getirildi ve EF Core migration (`MakeTmdbIdUniquePerMediaType`) uygulandı.
6. **Redis Blacklist Hata Politikası & .env SSOT Senkronizasyonu:** Token kara listeleme yazma hataları artık sessizce yutulmayıp `throw` edilerek Fail-Closed yapıldı; okuma hatalarında ise sistemin ayakta kalması için Fail-Open ve uyarı loglaması korundu. `docker-compose.yml` içindeki `vault_init` ortam değişkenleri eksiksiz eşitlendi.
7. **Performans Muhafızı (Zero Alloc & Timeout):**
   - `MediaRepository.GetAdminMediaPagedListAsync` sorgusuna `.AsNoTracking()` eklendi.
   - `JwtTokenService` içinde tek bir `TimeProvider.GetUtcNow()` kullanıldı, `SigningCredentials` nesnesi constructor'da önbelleğe alındı ve HMAC-SHA256 için 32-byte anahtar boyutu startup'ta doğrulandı.
   - `RedisCatalogCacheService` içinde ara UTF-16 string tahsisleri kaldırılarak doğrudan `(byte[])` UTF-8 deserialize ve `SerializeToUtf8Bytes` yapıldı; mükerrer okuma kodları tek yardımcı metoda toplandı.
   - `OutboxBatchProcessor` içerisinde RabbitMQ yayınlarına 10 saniyelik linked timeout eklendi, böylece soket takılmalarında veritabanı transaction'ının kilitli kalması önlendi.
8. **Ponytail Sadeleştirmeleri & Auth Response Servisi:**
   - Canlı çağıranı olmayan ölü MediatR sınıfları (`DomainEventNotification.cs`, `MediatorExtensions.cs`) ve testleri tamamen silindi (-95 satır).
   - 5 auth handler'da tekrarlanan 20'şer satırlık token üretimi, hashing ve permission çözümleme akışı `IAuthResponseService` servisine toplanarak Clean Code / SRP sağlandı (-80+ satır).
   - `ProcessOutboxMessagesJob` içindeki 9 taksonomi case bloğu tuple switch pattern ile 14 satıra indirgendi (-20 satır).
   - `scripts/scaffold_service.py` şablonları `Vault`, `IUnitOfWork` ve ortak `OutboxMessageConfiguration` standartlarına hizalandı.

---

## 2. Neden Yaptık?
- **Kimlik ve Oturum Bütünlüğü (OWASP API2):** Önceden açılmış e-posta hesaplarına yetkisiz sızan saldırganların 14 günlük refresh token'larının hayatta kalması en büyük hesap ele geçirme risklerinden biriydi.
- **Dağıtık Veri Bütünlüğü:** Downstream servislerin (ShuffleEngine, History) `EpisodeId = Guid.Empty` olan event'leri tüketmesi durumunda ilişkisel veri tabanında yabancı anahtar (FK) ihlalleri veya öksüz (orphan) kayıtlar oluşacaktı.
- **DDoS ve Brute-Force Savunması (OWASP API4):** Saldırganların saniyede sınırsız misafir token alarak tek IP'den auth uç noktalarını boğması engellendi.
- **Yasal Uyumluluk (KVKK / GDPR):** Kullanıcı hesabını sildiğinde açık metin IP veya sosyal sağlayıcı e-postalarının veritabanında kalması yasal cezalar doğurabilirdi.
- **Kaynak Tüketimi ve GC Baskısı:** Yüksek frekanslı Redis cache ve JWT operasyonlarında bellek tahsislerinin (memory allocation) en aza indirilmesi mikroservis yanıt sürelerini milisaniye seviyesinde tutar.

---

## 3. Hangi Pattern'leri Kullandık?
- **Single Responsibility Principle (SRP) & Service Extraction:** `IAuthResponseService` ile auth token üretimi ve persistence tek merkezde toplandı.
- **Transactional Outbox & InBox Pattern:** Mesajların çift yazma (dual-write) hatası olmadan ve timeout güvencesiyle broker'a basılması sağlandı.
- **Deferred Domain Event Pattern:** EF Core tarafından otomatik üretilen kimliklerin (Identity/GUID) SaveChanges öncesi kaybolmaması için entity üzerinde bekleyen event mekanizması uygulandı.
- **Composite Unique Index Pattern:** TPH hiyerarşisinde varlık tipine göre tekillik (`MediaType + TmdbId`) ayrıştırıldı.
- **Fail-Fast & Security by Design:** JWT gizli anahtar uzunluğu (256-bit) startup zamanında katı olarak doğrulandı.

---

## 4. Nasıl Test Edilir?
Projedeki tüm test katmanları terminal üzerinden çalıştırılarak doğrulanabilir:

```bash
# 1. Tüm Birim, Domain ve Mimari Testler (1,022 test)
dotnet test ShuffleSeries.sln

# 2. Kod Formatı ve Stil Doğrulaması
dotnet format ShuffleSeries.sln --verify-no-changes

# 3. YARP Gateway Üzerinden Uçtan Uca (E2E) Test Süitleri (216 test)
python3 e2e_tests/run_all.py
```

### Test Case Matrisi:
1. **Social Login Link & Token Revocation:** Önceden aktif refresh token'ı bulunan kullanıcı ilk kez Google/Apple ile bağlandığında `RevokedAllRefreshTokens` çağrılmalı ve yeni JWT `401` almadan başarıyla dönmelidir.
2. **Episode Created Event ID Testi:** `CreateEpisodeCommand` işletildiğinde Outbox tablosundaki JSON mesajının `EpisodeId` değeri kesinlikle `Guid.Empty` olmamalı ve aynı transaction'da mükerrer `EpisodeUpdatedDomainEvent` oluşmamalıdır.
3. **StrictLimiter IP Bounded Rate Limit:** Farklı JWT token'ları ile aynı IP üzerinden saniyede 5'ten fazla istek gönderildiğinde 6. istek anında `429 Too Many Requests` ProblemDetails almalıdır.
4. **GDPR PII Silinmesi:** `DeleteAccountCommand` sonrasında `UserLogins.ProviderKey` anonimleştirilmeli (`deleted_{id}`), `ProviderEmail` `null` olmalı ve `RefreshTokens.CreatedByIp` adresleri tamamen temizlenmelidir.
5. **TMDB ID Çakışma İzolasyonu:** Aynı `TmdbId` değerine sahip bir Series ve bir Movie başarıyla oluşturulabilmeli; aynı tipe sahip iki kayıt ise `409 DuplicateTmdbIdException` almalıdır.
