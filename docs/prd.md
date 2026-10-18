Shuffle Series - İşlevsel Gereksinimler Belgesi (PRD)

1. Ürün Özeti ve Kapsam
Shuffle Series, kullanıcıların film ve dizi seçerken yaşadıkları karar yorgunluğunu (decision fatigue) ortadan kaldırmak amacıyla tasarlanmış, oyunlaştırma ve "kaydırma" (swipe) mekaniklerine dayalı premium bir mobil uygulamadır. Sistem; kullanıcıların ruh hallerine, sahip oldukları dijital yayın platformlarına ve izleme geçmişlerine göre en uygun içeriği saniyeler içinde sunmayı hedefler.
Kapsamın en yenilikçi ve temel yapı taşlarından biri Bölüm Ruleti modülüdür. Bu modül; uzun soluklu bir diziye başlama taahhüdü (commitment) vermek istemeyen veya sadece kısıtlı vakti olan izleyiciler için tasarlanmıştır. Black Mirror, Love Death & Robots, Cabinet of Curiosities gibi her bölümü bağımsız bir hikaye anlatan antoloji serilerini ve Friends, The Office, Seinfeld, Rick and Morty gibi hikaye devamlılığı zorunluluğu olmayan durum komedisi / bağımsız tüketilebilir bölümleri (Standalone / Episodic content) bir havuza toplayarak, kullanıcıya bağlamdan kopuk, anında tüketilebilir, 20-45 dakikalık yüksek kaliteli "çerezlik" bir eğlence sunmayı amaçlar. Böylece kullanıcı "Hangi diziye başlasam?" yükünden tamamen kurtulur.

2. Kullanıcı Profilleri (Personalar)
Profil 1: Zamanı Kısıtlı İzleyici (Hızlı Tüketici)
Özellikler: İşten yorgun döner, sadece yemek yerken veya uyumadan önce 1 saatlik boş vakti vardır.
Davranış: Uzun sezonlu dizilere başlamaktan kaçınır. Vaktini platformların arama ekranlarında harcamaktan nefret eder.
Uygulama Kullanımı: Uygulamanın birincil hedef kitlesidir. Çoğunlukla Bölüm Ruleti ve Film Gecesi modlarını kullanarak saniyeler içinde izleyeceği içeriği bulur. "Sola kaydırarak (pas geçerek)" hızlı keşif yapmayı sever.
Profil 2: Kararsız Maratoncu (Duygu Odaklı İzleyici)
Özellikler: Hafta sonları veya uzun tatillerde ekran başında saatlerini geçirmeyi sever ancak ne izleyeceğine karar veremez.
Davranış: Türlerden ziyade "o anki moduna" göre içerik arar.
Uygulama Kullanımı: Dizi Keşfet modunu ve ana ekrandaki "Kafa Dağıtmalık", "Beyin Yakanlar" gibi his odaklı filtreleri yoğun olarak kullanır.
Profil 3: Arşivci Sinefil (Potansiyel Premium Abone)
Özellikler: İzlediği her şeyi listelemekten, puanlamaktan ve kendi sinema kütüphanesini oluşturmaktan keyif alır.
Davranış: Tavsiyelere açıktır ancak kendi kontrol alanında kalmayı da sever. Reklamlardan ve akışının bölünmesinden hoşlanmaz.
Uygulama Kullanımı: Listem ekranının aktif kullanıcısıdır. Algoritmayı eğitmek için "İzlediklerim" sekmesinde düzenli oylama yapar. "Kendi listemden shuffle yap" özelliği ve reklamsız deneyim için Premium pakete dönüşme potansiyeli en yüksek profildir.

3. Use Case (Kullanım Durumu) Modelleri ve Senaryolar
Aktörler:
Misafir Kullanıcı: Uygulamayı indirip onboarding akışını tamamlamış, kayıt adımında "Misafir Olarak Devam Et" seçeneğini seçerek 3 çark hakkıyla ana sayfaya ulaşmış kullanıcı.
Kayıtlı Kullanıcı: Onboarding sürecini tamamlayıp SSO/e-posta ile hesabını oluşturmuş, ücretsiz bilet ekonomisiyle sistemi kullanan kullanıcı.
Premium Kullanıcı: Sınırsız çark hakkı ve özel liste ruleti yetkisi olan ücretli abone.
Sistem Algoritması: Platform eşleşmesi, ruh hali filtresi ve oylama geçmişine göre havuzdan kart çeken arka plan servisi.
Temel Kullanım Senaryoları (Use Cases):
UC-01: Onboarding ve İlk Temas (Misafir Kullanıcı Girişi)
Aktör: Misafir Kullanıcı
Akış: Kullanıcı uygulamayı açar ve kademeli onboarding akışına girer (Ruh Hali Modu ve Platform seçimleri). Onboarding'in son adımındaki hesap oluşturma ekranında "Misafir Olarak Devam Et" butonuna basar. Sistem, kullanıcıyı 3 çark (bilet) hakkı ve 30 günlük kayan ömürlü (30-day sliding TTL) anonim bir oturumla doğrudan ana sayfaya yönlendirir. Kullanıcı ana ekrandaki devasa "Shuffle" butonuna basar; sistem onboarding'deki tercihlere uygun rastgele bir içerik kartını ekrana getirir ve bilet sayacından 1 hak düşülür (Kalan: 2/3).
UC-02: İçerik Kaydetme ve Misafirlikten Kayıtlı Hesaba Geçiş
Aktör: Misafir Kullanıcı, Sistem Algoritması
Akış: Misafir kullanıcı, Swipe Arenası'na düşen içeriği beğenip sağa kaydırır veya 3 biletlik hakkını tüketir. Sistem, içeriğin/tercihlerin kaybolmaması ve 14 günlük sınırsız bilet avantajından yararlanılması için alt panelden (Bottom Sheet) "Hesap Oluştur" çağrısını sunar. Kullanıcı SSO (Apple/Google) veya e-posta ile kaydolur. Sistem, misafir oturumundaki verileri (30 gün TTL dolmadan önce) yeni açılan hesaba aktarır (merge) ve sağa kaydırılan içeriği kullanıcının "Listem" ekranına ekler.
UC-03: Ruh Haline Göre Keşif ve Pas Geçme
Aktör: Kayıtlı Kullanıcı, Sistem Algoritması
Akış: Kullanıcı "Tümü" menüsünden "Beyin Yakanlar" filtresini seçer ve çarkı çevirir. Gelen kartı beğenmez ve sola kaydırır (Pas Geç). Pas geçilen içerik kullanıcı için 24 saatlik soğuma süresine (Cooldown) alınır. Sistem, kesintisiz yeni bir öneri talep edildiği için bilet bakiyesinden 1 adet düşer ve anında (tekrar çark ekranına dönmeden) yeni bir "Beyin Yakanlar" kartını ekrana getirir.
UC-04: Kıtlık ve Sürtünme Kancası (Premium'a Teşvik)
Aktör: Kayıtlı Kullanıcı
Akış: Kullanıcının bilet bakiyesi "0" olduğunda çarkı çevirmek ister. Sistem alt panel açarak iki seçenek sunar: "Reklam İzle (+1 Bilet)" veya "Premium'a Geç". Kullanıcı reklam izlemeyi seçer, reklam bitiminde kart ekrana düşerken sistem 3 saniyelik bir bildirim gösterir: "Reklamlar sinema keyfini bölmesin. Tek tıkla Premium'a geç."
UC-05: Listemden Shuffle Yap (Ayrıcalık ve Boş Durum)
Aktör: Premium Kullanıcı
Akış: Kullanıcı, biriken "İzleyeceklerim" listesindeki kararsızlığı çözmek için sağ alt köşedeki "Listemden Shuffle Yap" (FAB) butonuna basar. Sistem önce listedeki içerik sayısını doğrular: Listede en az 3 içerik varsa rulet oluşturup sonucu ekrana getirir ve doğrudan "Hemen İzle" (Deep Link) butonunu aktif eder. Listede 3'ten az içerik varsa sistem rulet başlatmaz, kullanıcıya şık bir rehber bildirim göstererek ana keşfe yönlendirir: "Listenden shuffle yapabilmek için en az 3 içerik eklemelisin."
UC-06: Doğrudan Arama ve Listeye Ekleme
Aktör: Kayıtlı Kullanıcı, Sistem Algoritması
Akış: Kullanıcı alt menünün merkezindeki "Ara" butonuna basar. Aklındaki "Inception" filmini arama çubuğuna yazar. Sistem anlık olarak film kartını getirir. Kullanıcı kartın yanındaki "+" ikonuna basarak filmi doğrudan "Listem" ekranına ekler. Eğer kullanıcı "Inception" detayına girerse, sistem benzer ruh halindeki (Örn: Beyin Yakanlar) filmleri rulet formatında hemen altta tavsiye olarak sunar.
UC-07: Sosyal Paylaşım ve Viral Döngü (Kart Paylaşımı)
Aktör: Kayıtlı / Misafir Kullanıcı
Akış: Kullanıcı Swipe Arenası'nda beğendiği veya şaşırdığı bir kartı sağ üstteki paylaşım ikonuna dokunarak açar. Sistem, filmin afişi, IMDb puanı, ruh hali etiketi ve platform logolarını içeren estetik bir "Sinema Bileti" şablonunda görsel oluşturur (Share Sheet). Kullanıcı tek tıkla Instagram Story veya WhatsApp'a paylaşır; görselin altında ShuffleSeries derin bağlantısı (App link) yer alır.

4. Genel Tasarım ve UX Yönergeleri
Tema: OLED uyumlu derin siyah (pitch black) arka planlar ve yarı saydam (frosted glass) paneller.
Renk Paleti: Aktif modlara göre değişen dinamik ortam ışıkları (ambient blur).
Bölüm Ruleti: Yakut Kırmızısı (Ruby)
Dizi Keşfet: Derin Kobalt (Abyssal Blue)
Film Gecesi: Eskitilmiş Bronz (Aged Brass)
Ana Eylem Butonları (CTA): Kan Portakalı (Blood Orange) veya Titanyum Beyazı.
Mikro Etkileşimler: Tüm çark çevirme, ruh hali modu/platform seçme ve kart kaydırma eylemlerinde Apple ekosistemine benzer pürüzsüz dokunsal (haptic) titreşimler.

5. Fonksiyonel Modüller ve Ekranlar

5.1. Başlangıç ve Onboarding Modülü
Karşılama ve Kademeli Katılım (Gradual Engagement): Uygulamayı ilk kez açan her kullanıcı doğrudan onboarding akışıyla karşılanır. Yüksek çözünürlüklü, yavaşça hareket eden (slow-pan) flu afiş arka planı eşliğinde tercihler toplanır:
Adım 1 (Ruh Hali / Mod Seçimi): Üstte "1/2" ilerleme çubuğu. His odaklı özgün Ruh Hali Modları (Örn: Beyin Yakanlar, Kafa Dağıtmalık - altında ilgili klasik tür açıklamalarıyla) film/dizi kareleri (thumbnail grids) listelenir. Dokunulan kareler (haptic feedback ile) seçilir, istenmeyenler aşağı kaydırılarak elenir.
Adım 2 (Platform Seçimi): Üstte "2/2" ilerleme çubuğu. Netflix, Prime Video gibi logolar ızgara halinde sunulur. Popüler 2-3 platform varsayılan olarak seçili gelir.


Kayıt ve Giriş Adımı (Soft Sign-up / Guest Choice): Onboarding tercihlerinin hemen ardından hesap tamamlama ekranı açılır:
Apple ve Google SSO (Tek Tıkla Giriş) butonları birincil, e-posta seçeneği ikincil metin linki olarak sunulur.
Misafir Modu Seçeneği: Ekranın alt kısmında belirgin bir "Misafir Olarak Devam Et" butonu yer alır. Kullanıcı bu seçeneğe tıkladığında kayıt olmadan doğrudan Ana Sayfa'ya aktarılır, hesabına 3 çark hakkı (3/3 ⚡ Ücretsiz Bilet) tanımlanır ve anonim oturumu Redis/DB'de 30 günlük kayan ömürle (30-day sliding TTL) güvenceye alınır.


Kayıt olmayı tercih eden kullanıcılara ise 14 günlük sınırsız bilet hakkı (Balayı Dönemi) verilir; sonraki dönemlerde huni misali günlük bilet sayıları kademeli olarak düşer. Misafir kullanıcının daha sonra hesap oluşturması durumunda (merge) tüm geçmiş ve tercih verileri 30 gün içinde asıl hesaba aktarılır.


Kanca (Hook) Tetikleyicisi: Misafir kullanıcı çıkan ilk içeriği sağa kaydırdığında (beğendiğinde) veya 3 biletlik deneme hakkı bittiğinde alt kısımdan "Premium Panel (Bottom Sheet)" açılarak kayıt olma ve avantajlardan yararlanma süreci tetiklenir.

5.2. Ana Sayfa (Keşfet) Modülü
Üst Bar (Top Bar): Yarı saydam (glassmorphism) zemin üzerinde Bilet Bakiyesi (🎟️ - Misafir için "3/3", Kayıtlı için kurgulanmış bakiye), Seri Durumu (🔥) ve Profil Avatarı.
Mod Seçici (Pill Menu): Bölüm Ruleti, Dizi Keşfet ve Film Gecesi arasında geçiş sağlayan hap tasarım. Geçişlerde arka plan ambiyans rengi değişir.
Fırlatma Rampası (Idle State): Ekranın merkezinde nabız gibi atan büyük "Şansını Dene" butonu.
Ruh Hali İstasyonu: Çarkın altında hızlı erişim hapları (Genel, Kafa Dağıtmalık vb.). "Tümü" ikonuna tıklandığında alttan açılan (bottom sheet) kapsamlı ruh hali paneli (grid listeleme).

5.3. Sinematik Aksiyon ve Swipe Arenası
Tetiklenme (Rulet Çevirme): Çarka basıldığında bilet sayacından 1 adet düşer, ekran kararır, telefon haptic titreşimle tepki verir ve "Netflix Hero Afişi" standartlarında tam ekran içerik kartı (rounded corners) merkeze oturur. Kart üzerinde IMDb puanı, klasik türler (Genre: Bilim Kurgu, Gerilim vb.), ruh hali etiketi (Mood: Beyin Yakanlar vb.), platform logoları, özet bilgisi ve sağ üst köşede sosyal paylaşım ikonu (Share Sheet) bulunur.
Sola Kaydırma (Pas Geç): İçerik silikleşerek kaybolur. Bu içerik kullanıcı için 24 saatlik soğuma süresine (Cooldown) alınır ve gün boyunca tekrar gösterilmez. Kesintisiz akışla yeni bir kart talep edildiği için bilet sayacından 1 adet düşülür ve kesintisiz akışla yeni bir öneri kartı ekrana gelir. (Eğer bilet sıfırsa "Kıtlık Kancası" devreye girer).
Sağa Kaydırma (Listeme Ekle): Kart tok bir yeşil ton ile onaylanır. İçerik "İzleyeceklerim" listesine eklenir ve ana rulet havuzundan kalıcı olarak çıkarılır (aynı kullanıcıya rulette tekrar önerilmez). Başarı anı (Aha! moment) olduğu için ekstra bilet düşülmez. Ekranda "Listene Eklendi!" mesajı, cihazda yüklü platform uygulamasını doğrudan ilgili içeriğin oynatma sayfasında açan derin bağlantılı (Deep Link / Universal Link: nflx://, primevideo:// vb.) devasa "Hemen İzle" butonu, Listem ekranına yönlendirme butonu ve yeni bir tur için "Keşfetmeye Devam Et (1 Bilet)" butonu belirir.
Performans ve Ön Yükleme (Prefetching & Latency Buffer): Mobil istemci, 0 ms gecikmeli pürüzsüz kart kaydırma deneyimi için arka planda sıradaki 3-5 öneri kartını önceden belleğe (cache/buffer) yükler.
Havuz Tükenmesi ve Sıfır Sonuç (Zero-Result State & Fallback): Seçilen platform ve ruh hali kombinasyonunda gösterilecek içerik kalmadığında (veya tümü 24 saatlik soğumadaysa), sistem kullanıcıya beyaz ekran yerine akıllı öneri sunar: "Seçtiğin platformlarda bu modda içerik kalmadı. Platform filtreni genişletmek veya en yakın mod olan [Benzer Mod]'dan devam etmek ister misin?"
Sosyal Paylaşım (Share Sheet): Kart üzerindeki paylaşım ikonuna dokunulduğunda, yapımın afişi, puanı ve mod etiketini içeren şık bir "Sinema Bileti" şablonu üretilir; tek tıkla Instagram Story veya WhatsApp'a aktarılır.
Kapatma Jesti: Kart hızla aşağı çekilerek (pull-down-to-close) arena kapatılır ve ana fırlatma rampasına dönülür.

5.4. Listem Modülü
Navigasyon ve Filtreler: "İzleyeceklerim" ve "İzlediklerim" ana sekmeleri. Altında "Bölümler", "Filmler" gibi yatay kaydırılabilir alt kategori çipleri.
Kart UI (List Item): 16:9 yatay (landscape) ve kompakt afiş kartları. Üzerinde platform logosu, klasik türü (Genre) ve eklenme sebebini belirten ruh hali modu etiketi (Mood Tag).
Etkileşimler:
Karta tıklandığında detay sayfası ve "Platformda Aç" butonu.
Liste içinde sağa kaydırma: İçeriği "İzlediklerim" sekmesine taşır.
Liste içinde sola kaydırma: İçeriği listeden siler (fade-out efekti).
Oylama Sistemi ve Algoritma Geri Beslemesi (Weight Feedback): İzlediklerim sekmesine düşen içerikler "Çift Başparmak / Beğendim / Beğenmedim" formatında oylanır:
- Çift Başparmak: Yapımın tür, yönetmen ve etiketlerine +%30 ağırlık puanı kazandırır (benzer yapımlar rulet havuzunda önceliklendirilir).
- Beğendim: +%10 hafif pozitif etki sunar.
- Beğenmedim: Yapımın tür ve temalarına -%50 ceza katsayısı uygular (benzer yapımların rulette çıkma olasılığı bastırılır).
Liste İçi Shuffle (FAB): Sağ alt köşede süzülen siyah zeminli "Listemden Shuffle Yap" butonu (Premium ile entegre çalışır).
- Kural: Listede en az 3 içerik bulunması zorunludur. 3'ten az içerik varken basıldığında rulet başlamaz; kullanıcıya rehber bildirim gösterilir: "Listenden shuffle yapabilmek için en az 3 yapım eklemelisin."
Boş Durum (Empty State): "Burası çok sessiz" metni, minimalist sinema koltuğu silüeti ve ana sayfaya yönlendiren CTA butonu.

5.5. Profil Modülü
Kullanıcı Kimliği: Avatar, kazanılmış en yüksek ünvan (Örn: Antoloji Avcısı) ve kırmızı/beyaz oynatma çubuğu zarafetinde XP (Seviye) ilerleme çubuğu.
Dashboard (İstatistikler): Keşif sayısı, bilet durumu ve aylık izleme alışkanlıkları liste formatında. Ücretsiz kullanıcılar için pasif gri renkte "Reklamsız İzleme" metriği.
Rozetler Vitrini: 3D tasarımlı premium ödül ikonları yatay bir carousel'de sergilenir. Instagram Story paylaşım entegrasyonu.
Ayarlar:
"Ruh Hali Profilim" ve "Platformlarım": Dokunulduğunda onboarding ızgarası açılır ve güncellenir.
Bildirimler: iOS sistem ayarları tarzında aç/kapat (toggle) butonları ile yönetilir (Favori ruh hali bildirimleri, Seri hatırlatıcı, Hafta sonu sürprizi).

5.6. Arama (Keşif ve Arama) Modülü
Bu modül, ruletin getirdiği sürpriz faktörüne ek olarak, kullanıcının aklında belirli bir içerik olduğunda doğrudan hedefe ulaşmasını sağlar.
Arama Arayüzü (Search UI): Ortadaki büyüteç ikonuna tıklandığında ekran aşağıdan yukarıya pürüzsüz bir animasyonla açılır (modal sheet). Arka plan koyu füme (pitch black) tonlarındadır. Üst kısımda, klavye açıldığında aktifleşen geniş ve yarı saydam (frosted glass) bir arama çubuğu yer alır.
Boş Durum (Idle State): Kullanıcı henüz arama yapmadan önce ekranda iki yatay liste sunulur:
Son Arananlar: Kullanıcının daha önceki aramaları (silinebilir çip formatında).
Günün Popüler Ruletleri: Diğer kullanıcıların o gün en çok listesine eklediği 3-4 trend yapım (mini kartlar halinde).
Dinamik Sonuçlandırma (Live Search): Kullanıcı yazmaya başladığı an (debounce mantığıyla) sonuçlar anlık olarak dökülür. Sonuçlar karmaşayı önlemek adına sekmelere veya dikey gruplara ayrılır: Tümü, Diziler, Filmler, Bölümler (Antolojiler). Ayrıca arama ekranında klasik türler (Genre: Aksiyon, Komedi, Bilim Kurgu vb.) ve ruh halleri (Mood: Beyin Yakanlar, Kafa Dağıtmalık vb.) çoklu filtreleme (facet filtering) kriteri olarak sunulur.
Kart ve Etkileşim (Search Result Item): Arama sonuçları, "Listem" ekranındaki gibi 16:9 yatay ve kompakt kartlar halinde listelenir.
Kartın sağ tarafında hızlı bir "Listeme Ekle (+)" ikonu bulunur. Kullanıcı rulet çevirmeden içeriği direkt "İzleyeceklerim" sekmesine atabilir.
Eğer aranan içerik Black Mirror gibi bir antoloji dizisiyse, kartın üzerinde parlayan özel bir "Sadece Bu Diziyi Shuffle Yap" butonu belirir (İleri seviye özellik).
Platform Filtresi Entegrasyonu: Kullanıcı arama yaptığında, sonuçlarda içeriğin hangi platformda olduğu logosuyla belirtilir. Eğer içerik kullanıcının sahip olmadığı bir platformdaysa, logo gri (pasif) görünür ve üzerine tıklandığında "Bu içerik platformlarında yok ama sana benzerlerini bulabiliriz" diyerek kullanıcıyı tekrar ana sayfadaki rulet mekanizmasına (veya Premium'a) yönlendiren bir akıllı köprü (smart bridge) kurulur.

6. Akıllı Rulet ve Öneri Motoru Mimarisi (Shuffle Engine)
Shuffle Engine, kullanıcının karar yorgunluğunu ortadan kaldırırken hem kişiselleştirilmiş alaka düzeyini (relevance) hem de rulet heyecanını ve şaşırtıcılığını (serendipity) saniyeler içinde sunan çok aşamalı (multi-stage) bir öneri motorudur.

6.1. Girdi Sinyalleri (Input Signals)
Motor, her rulet çevirme isteğinde 4 farklı veri katmanını değerlendirir:
1. Statik Kullanıcı Tercihleri: Kullanıcının sahip olduğu dijital yayın platformları (Netflix, Prime vb.) ve onboarding aşamasında seçtiği favori görsel kareler/modlar.
2. Dinamik Oturum Parametreleri: Seçilen aktif mod (Bölüm Ruleti / Dizi Keşfet / Film Gecesi), seçilen anlık ruh hali (Mood: Kafa Dağıtmalık vb.) ve oturum bağlamı (hafta içi akşam vs. hafta sonu gündüz).
3. Davranışsal ve Tarihsel Geçmiş:
   - "İzleyeceklerim" ve "İzlediklerim" listesindeki yapımlar.
   - Son 24 saat içinde sola kaydırılarak pas geçilen yapımlar (24h Cooldown Set).
   - "İzlediklerim" sekmesinde verilen oylar (Çift Başparmak, Beğendim, Beğenmedim).
   - "Hemen İzle" butonuna tıklanma geçmişi ve etkileşim sıklığı.
4. Katalog ve İçerik Metadataları: IMDb puanı, TMDB popülerlik/oy sayısı metrikleri, yapım yılı/yenilik (recency), süre (duration - örn. 45 dk altı antoloji), yönetmen/oyuncu kadrosu ve klasik türler (Genre).

6.2. Çok Aşamalı Öneri Boru Hattı (3-Stage Pipeline)
Algoritma saf bir rastgele seçim yapmaz; 3 aşamalı bir süzgeçten geçer:

Aşama 1: Sert Filtreleme (Candidate Generation / Hard Filtering)
- Platform Filtresi: Yalnızca kullanıcının profilinde aktif olan platformlarda yayınlanan içerikler havuza alınır (MediaPlatforms).
- Mod Filtresi: Seçilen moda göre Movie, Series veya Episode.IsStandalone == true olan yapımlar ayrıştırılır.
- Ruh Hali Filtresi: Yalnızca kullanıcının seçtiği Mood etiketine sahip yapımlar (MediaMoods) çekilir.
- Sert Muafiyetler (Hard Exclusions):
  * Kullanıcının "İzleyeceklerim" veya "İzlediklerim" listesindeki tüm içerikler rulet havuzundan kalıcı olarak çıkarılır.
  * Kullanıcının son 24 saat içinde sola kaydırdığı (pas geçtiği) yapımlar Redis Cooldown setinden kontrol edilerek elenir.

Aşama 2: Ağırlıklı Uygunluk Skorlaması (Suitability Scoring)
Filtreleri geçen aday havuzundaki her içeriğe aşağıdaki formülle bir Uygunluk Skoru (S) atanır:
$$S = \text{Temel Kalite Skoru} \times (1 + \text{Kullanıcı İlgi Katsayısı}) \times \text{Ceza Katsayısı}$$
- Temel Kalite Skoru: $(\text{IMDb Puanı} \times 0.5) + (\text{Popülerlik Metriği} \times 0.3) + (\text{Yenilik Çarpanı} \times 0.2)$.
- Kullanıcı İlgi Katsayısı (Affinity Boost):
  * İçeriğin türü, yönetmeni veya başrol oyuncusu, kullanıcının daha önce "Çift Başparmak" ile oyladığı bir yapımla eşleşiyorsa: +%30 Boost.
  * İçerik, kullanıcının onboarding'de dokunduğu görsel tercihlerle örtüşüyorsa: +%15 Boost.
- Ceza Katsayısı (Penalty Factor):
  * İçeriğin teması veya türü, kullanıcının daha önce "Beğenmedim" dediği yapımlarla benzeşiyorsa: -%50 Ceza Katsayısı uygulanır.

Aşama 3: Ağırlıklı Rastgele Seçim (Weighted Random / Serendipity Selection)
- Rulet ruhunu korumak ve öngörülebilirliği (sıkıcılığı) kırmak amacıyla sistem asla en yüksek puanlı 1 numaralı içeriği doğrudan getirmez.
- Adaylar, hesaplanan skorları (S) oranında bir olasılık dağılımına (Roulette-Wheel / Softmax Selection) tabi tutulur:
  * Yüksek skorlu başyapıtların gelme olasılığı çok daha yüksektir (havuzun %70-80'lik dilimi).
  * Ancak kaliteli fakat az bilinen sürpriz yapımların (hidden gems) çıkma şansı da (%20-30) canlı tutulur. Böylece kullanıcı her çark çevirişte hem güvenli bir öneri alır hem de keşif hazzını yaşar.

6.3. Soğuk Başlangıç Stratejisi (Cold-Start Problem)
Henüz izleme veya oylama geçmişi bulunmayan misafir veya yeni kayıtlı kullanıcılar için:
- Onboarding'in 1. adımında dokunulan thumbnail karelerinden ilk ilgi vektörü oluşturulur.
- IMDb puanı 7.5 ve üzeri olan, platformların en popüler içeriklerinden oluşan "Güvenli Başyapıtlar Havuzu" (Safety Net Pool) devreye sokulur; kullanıcının ilk deneyimde zayıf bir içerikle karşılaşması önlenir.

6.4. Sürekli Öğrenme ve Olay Güdümlü Geri Besleme (Feedback Loop)
- Kullanıcı bir kartı sağa veya sola kaydırdığında ya da oyladığında, sistem arka planda Domain/Integration Event'leri (CardSkippedEvent, CardAddedToWatchlistEvent, MediaVotedEvent) fırlatır.
- Shuffle Engine, RabbitMQ üzerinden bu event'leri dinleyerek kullanıcının Redis üzerindeki İlgi Vektörünü (Feature Vector) asenkron olarak günceller; böylece rulet motoru her kullanımda daha kişisel ve isabetli hale gelir.

7. Premium Abonelik ve Kanca (Hook) Stratejileri
Kıtlık Kancası: Bilet sıfırlandığında ruleti çevirme denemesinde alt panel açılır: "Kısa Bir Reklam İzle (+1 Bilet)" veya ana neon renkte "Premium'a Geç (Sınırsız & Reklamsız)".
Sürtünme Kancası: Videolu reklam bitip içerik kartı düştüğünde ekranda beliren geçici (3 saniye) kapsül bildirim: "Reklamlar sinema keyfini bölmesin. Tek tıkla Premium'a geç."
Ayrıcalık Kancası (Gating): "Listemden Shuffle Yap" FAB butonu kilitlidir (locked). Tıklandığında liste bulanıklaşır ve premium yönlendirmesi çıkar.
Statü Kancası: Profil istatistiklerinde kilitli duran altın/bronz "Sınırsız Sinefil" rozetine tıklama eylemi.
Derinlik Kancası: Ruh hali istasyonundaki "Tümü" panelinde yer alan spesifik filtrelere (Örn: Sadece 8.0+ IMDb Puanlılar) taç/kilit ikonu konur; dokunulduğunda paywall tetiklenir.
Etkinlik Kancası (FOMO): Cuma akşamları gelen sürpriz bilet bildirimine tıklandığında açılan uygulamada, Premium üyelere özel "Hafta Sonu Garantili Başyapıtlar" VIP arenasını tanıtan afiş (banner) gösterilir.

8. Tutundurma (Retention) ve Bilet Ekonomisi (Boiling Frog Stratejisi)
Balayı Dönemi (İlk 14 Gün / 2 Hafta): Kayıt olan kullanıcıya ilk 14 gün boyunca bilet sayacı üst barda "Sınırsız" (♾️) gösterilir. Arka planda kullanıcının listesine en az 10 içerik eklemesi ve Listemden Shuffle yapması hedeflenir.
Kademeli Daralma (Tatlı Sürtünme): 14 günlük balayı dönemi tamamlandığında üst bar bildirimi düşer: "Hoş geldin ayrıcalıklarının son günü. Günlük 15 ücretsiz biletin tanımlandı." Bilet azaldıkça sayaç ikonu nötrden sıcak (kehribar/turuncu) renge geçer.
Ödül/Telafi Mekanizmaları:
İzlediklerim sekmesinde 3 içerik puanlandığında veya anket doldurulduğunda "+3 Özel Bilet" hediye edilir.
Uygulamaya 7 gün üst üste giriş yapanlara "Sadakat Bileti" verilir.

9. Kısıtlamalar ve Taksonomi Mimarisi (Mood vs. Genre)
Sistemde içerik sınıflandırması ve keşfi iki temel eksende (Dual-Taxonomy) yürütülür:
1. Klasik Türler (Genre): TMDB standardında, evrensel ve nesnel film/dizi türleridir (Aksiyon, Komedi, Bilim Kurgu, Gerilim vb.). Arama (Search Service) ve detaylı katalog filtrelemelerinde kullanılır.
2. Ruh Hali Modları (Mood): Kullanıcı psikolojisine ve anlık hissiyatına odaklanan özgün deneyim havuzlarıdır. Shuffle Engine rulet algoritmasının ana itici gücüdür. Ruh halleri klasik tür isimleriyle değil, özgün his başlıklarıyla sunulur ve altında hangi klasik türleri kapsadığı açıkça belirtilir:
- Kafa Dağıtmalık: Çerezlik, yormayan komedi ve sitcomlar.
- İçini Isıtanlar: Sıcak romantizm, dostluk, aile ve moral düzelten naif hikayeler.
- Beyin Yakanlar: Karmaşık kurgulu, zaman yolculuğu veya mind-fuck filmler/diziler.
- Zeka Oyunları: Katil kim bulmacaları (whodunnit), dedektiflik, organize suç ve akıl almaz soygunlar.
- Gaza Getirenler: Yüksek aksiyon, intikam ve dövüş odaklı yapımlar.
- Gerim Gerim Gerenler: Yüksek tansiyonlu thriller ve korku yapımları.
- Dünyadan Uzaklaş: Fantastik evrenler, epik bilimkurgular.
- Gözyaşı Garantili: Yoğun dram ve duygusal hikayeler.
- Ufkunu Genişlet: İlham verici belgeseller, biyografiler, tarih ve gerçek hayat hikayeleri.

10. Teknik ve Operasyonel Kurallar Özeti (Mimari Kriterler)
| Kriter / Parametre | Değer / Kural | Açıklama |
| :--- | :--- | :--- |
| **Bilet Tüketim Kuralı** | 1 Bilet / Yeni Öneri | Çarka basıldığında ve pas geçilip yeni kart istendiğinde 1 bilet düşer; sağa kaydırıp listeye eklemek bedavadır (0 bilet). |
| **Misafir Oturumu (Guest TTL)** | 30 Gün (Sliding) | Cihazını değiştirmeyen veya silmeyen misafir kullanıcının tercihleri ve biletleri 30 gün boyunca korunur. |
| **Pas Geçme Soğuma Süresi** | 24 Saat (1 Gün) | Sola kaydırılan içerik 24 saat boyunca aynı kullanıcıya tekrar önerilmez. |
| **Listeye Eklenen İçerik** | Kalıcı Muafiyet | Sağa kaydırılan veya listeye eklenen yapımlar ana rulet havuzundan tamamen çıkarılır. |
| **Listemden Shuffle Barajı** | Minimum 3 İçerik | İzleyeceklerim listesinde 3'ten az içerik varsa rulet başlatılmaz, boş durum rehberi açılır. |
| **Derin Bağlantı (Deep Link)** | Native App Scheme -> Web Fallback | "Hemen İzle" butonu cihazda yüklü platform uygulamasını (`nflx://` vb.) doğrudan ilgili içerikte açar. |
| **Ön Yükleme (Prefetch Buffer)** | 3 - 5 Kart | İstemci sıfır gecikmeli kaydırma için sonraki kartları arka planda önceden belleğe alır. |
| **Sıfır Sonuç (Zero-Result)** | Akıllı Gevşetme (Fallback) | Havuz tükendiğinde beyaz ekran yerine en yakın benzer mod veya platform genişletme önerisi sunulur. |
| **Oylama Ağırlıkları (Weight Feedback)** | +%30 / +%10 / -%50 | Çift Başparmak (+%30), Beğendim (+%10), Beğenmedim (-%50 ceza katsayısı) algoritmayı besler. |
| **Seçim Dağılımı (Sampling)** | Rulet Çarkı (Weighted Random) | En skorer kart yerine olasılık ağırlıklı seçim yapılır (%70-80 top tier, %20-30 hidden gem). |
