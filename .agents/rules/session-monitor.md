# Session Token & Context Monitor

Bu kural, kullanıcının bağlam (context window) doluluğunu rahatça takip edebilmesi ve gerektiğinde `/compact` yapabilmesi amacıyla tanımlanmıştır.

## Temel İlkeler ve Davranış Standartları

1. **Bağlam ve Token Bilinci:**
   - Asistan, oturum boyunca aktif context window doluluğunu (`transcript.jsonl` metrikleri üzerinden) izler.
   - Standart Gemini modelleri (Flash / Pro) ~1.000.000 token kapasiteye sahiptir.

2. **Her Yanıtta İki Kademeli Durum Rozeti (Session & Context Badge - ZORUNLU):**
   - Asistan, her yanıtın veya kod geliştirme adımının en altına istisnasız aşağıdaki iki satırlı rozeti ekler:
     > 📊 **Aktif Bağlam:** ~Xk / 1M Token (%Y) | **Compact:** Gerekmiyor (veya Önerilir)  
     > 📈 **Kümülatif Oturum:** ~Zk Token
   - **Aktif Bağlam (Active Window):** O an modele gönderilen anlık aktif prompt ve bellek yükünü (özet + kurallar + son adımlar) gösterir. Otomatik budama (truncation/compaction) sonrası düşebilir.
   - **Kümülatif Oturum (Cumulative Usage):** Oturumun başından itibaren üretilen ve tüketilen toplam token hacmini temsil eder; monoton olarak daima artar.

3. **Erken Uyarı & Proaktif Compact Hatırlatması (%70 Eşiği):**
   - Oturum doluluğu **%70** seviyesine yaklaştığında veya oturumda devasa dosya okumaları/uzun terminal logları biriktiğinde, kullanıcıya proaktif olarak:
     > ⚠️ **Bağlam Uyarısı:** Oturum doluluğu %70 eşiğine ulaştı. Modelin dikkat dağınıklığını önlemek ve hafızayı tazelemek için lütfen sohbet kutusuna **/compact** yazınız.
     uyarısını ver.

4. **Görsel Arayüz (Generative UI / Side Pane):**
   - Kullanıcı "monitörü göster", "oturum durumu", "doluluk paneli" gibi isteklerde bulunduğunda, Tailwind ile tasarlanmış görsel `session_monitor.html` widget'ını ilgili oturumun artifact dizinine yaz ve `<agent-embed>` ile kullanıcıya sun.
