using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Catalog.Domain.Enums;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence;

public static class CatalogDbSeeder
{
    public static async Task SeedAsync(CatalogDbContext context, CancellationToken cancellationToken = default)
    {
        await SeedMoodsAsync(context, cancellationToken);
        await SeedPlatformsAsync(context, cancellationToken);
        await SeedGenresAsync(context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        await SeedSampleMediaAsync(context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedMoodsAsync(CatalogDbContext context, CancellationToken cancellationToken)
    {
        var existingCodes = await context.Moods
            .IgnoreQueryFilters()
            .Select(m => m.Code)
            .ToListAsync(cancellationToken);

        var moodsToSeed = new List<(string Code, string Color, string Icon, int Order,
            (string TrName, string TrDesc, string TrTags),
            (string EnName, string EnDesc, string EnTags))>
        {
            ("kafa-dagitmalik", "#F59E0B", "popcorn", 1,
                ("Kafa Dağıtmalık", "Çerezlik, yormayan komedi ve sitcomlar.", "Komedi, Sitcom, Eğlenceli"),
                ("Easygoing", "Bite-sized, effortless comedies and sitcoms.", "Comedy, Sitcom, Feel-Good")),

            ("icini-isitanlar", "#EC4899", "heart", 2,
                ("İçini Isıtanlar", "Sıcak romantizm, dostluk, aile ve moral düzelten naif hikayeler.", "Romantik, Aile, İyi Hissettiren"),
                ("Heartwarming", "Warm romance, friendship, family, and feel-good uplifting stories.", "Romance, Family, Feel-Good")),

            ("beyin-yakanlar", "#8B5CF6", "brain", 3,
                ("Beyin Yakanlar", "Karmaşık kurgulu, zaman yolculuğu veya mind-fuck yapımlar.", "Gizem, Bilimkurgu, Mind-Fuck"),
                ("Mind-Bending", "Intricate plots, time travel, and mind-twisting twists.", "Mystery, Sci-Fi, Mind-Fuck")),

            ("zeka-oyunlari", "#0D9488", "search", 4,
                ("Zeka Oyunları", "Katil kim bulmacaları, dedektiflik, organize suç ve akıl almaz soygunlar.", "Suç, Gizem, Dedektiflik"),
                ("Sleuth & Mystery", "Whodunnit puzzles, detective investigations, crime, and clever heists.", "Crime, Mystery, Detective")),

            ("gaza-getirenler", "#EA580C", "flame", 5,
                ("Gaza Getirenler", "Yüksek aksiyon, intikam ve dövüş odaklı yapımlar.", "Aksiyon, İntikam, Suç"),
                ("Pure Adrenaline", "High-octane action, vengeance, and martial arts.", "Action, Revenge, Crime")),

            ("gerim-gerim-gerenler", "#E0115F", "zap", 6,
                ("Gerim Gerim Gerenler", "Yüksek tansiyonlu thriller ve korku yapımları.", "Gerilim, Korku, Psikolojik"),
                ("Edge of Your Seat", "High-tension thrillers and nerve-racking horror.", "Thriller, Horror, Psychological")),

            ("dunyadan-uzaklas", "#10B981", "compass", 7,
                ("Dünyadan Uzaklaş", "Fantastik evrenler, epik bilimkurgular.", "Fantastik, Bilimkurgu, Macera"),
                ("Escape Reality", "Immersive fantasy realms and epic sci-fi adventures.", "Fantasy, Sci-Fi, Adventure")),

            ("gozyasi-garantili", "#0284C7", "droplet", 8,
                ("Gözyaşı Garantili", "Yoğun dram ve duygusal hikayeler.", "Dram, Romantik, Duygusal"),
                ("Tearjerkers", "Deeply touching, intensely emotional dramas.", "Drama, Romance, Emotional")),

            ("ufkunu-genislet", "#6366F1", "globe", 9,
                ("Ufkunu Genişlet", "İlham verici belgeseller, biyografiler, tarih ve gerçek hayat hikayeleri.", "Belgesel, Biyografi, Tarih"),
                ("Eye-Opening", "Inspiring documentaries, biographies, history, and captivating true stories.", "Documentary, Biography, History"))
        };

        foreach (var m in moodsToSeed)
        {
            if (existingCodes.Contains(m.Code))
                continue;

            var mood = Mood.Create(m.Code, m.Color, m.Icon, m.Order);
            mood.AddOrUpdateTranslation(LanguageCode.Turkish, m.Item5.TrName, m.Item5.TrDesc, m.Item5.TrTags);
            mood.AddOrUpdateTranslation(LanguageCode.English, m.Item6.EnName, m.Item6.EnDesc, m.Item6.EnTags);
            mood.ClearDomainEvents();

            context.Moods.Add(mood);
        }
    }

    private static async Task SeedPlatformsAsync(CatalogDbContext context, CancellationToken cancellationToken)
    {
        var existingSlugs = await context.Platforms
            .IgnoreQueryFilters()
            .Select(p => p.Slug)
            .ToListAsync(cancellationToken);

        var platformsToSeed = new List<(string Name, string Slug, string Logo, string InactiveLogo, string Web, string Scheme, int Order)>
        {
            ("Netflix", "netflix",
                "https://assets.shuffleseries.com/platforms/netflix.svg",
                "https://assets.shuffleseries.com/platforms/netflix-inactive.svg",
                "https://www.netflix.com", "netflix://", 1),

            ("Prime Video", "prime-video",
                "https://assets.shuffleseries.com/platforms/prime-video.svg",
                "https://assets.shuffleseries.com/platforms/prime-video-inactive.svg",
                "https://www.primevideo.com", "primevideo://", 2),

            ("Disney+", "disney-plus",
                "https://assets.shuffleseries.com/platforms/disney-plus.svg",
                "https://assets.shuffleseries.com/platforms/disney-plus-inactive.svg",
                "https://www.disneyplus.com", "disneyplus://", 3),

            ("Apple TV+", "apple-tv",
                "https://assets.shuffleseries.com/platforms/apple-tv.svg",
                "https://assets.shuffleseries.com/platforms/apple-tv-inactive.svg",
                "https://tv.apple.com", "videos://", 4),

            ("BluTV", "blutv",
                "https://assets.shuffleseries.com/platforms/blutv.svg",
                "https://assets.shuffleseries.com/platforms/blutv-inactive.svg",
                "https://www.blutv.com", "blutv://", 5)
        };

        foreach (var p in platformsToSeed)
        {
            if (existingSlugs.Contains(p.Slug))
                continue;

            var platform = Platform.Create(
                p.Name, p.Slug, p.Logo, p.InactiveLogo, p.Web, p.Scheme, p.Order);
            platform.ClearDomainEvents();

            context.Platforms.Add(platform);
        }
    }

    private static async Task SeedGenresAsync(CatalogDbContext context, CancellationToken cancellationToken)
    {
        var existingCodes = await context.Genres
            .IgnoreQueryFilters()
            .Select(g => g.Code)
            .ToListAsync(cancellationToken);

        var genresToSeed = new List<(string Code, int Order,
            (string TrName, string? TrDesc),
            (string EnName, string? EnDesc))>
        {
            ("action", 1, ("Aksiyon", "Yüksek tempolu ve heyecan dolu sahneler."), ("Action", "Fast-paced and thrilling action sequences.")),
            ("adventure", 2, ("Macera", "Bilinmeyene yolculuk ve keşif dolu serüvenler."), ("Adventure", "Journeys into the unknown and exploratory quests.")),
            ("animation", 3, ("Animasyon", "Çizim ve bilgisayar animasyonlarıyla yaratılan evrenler."), ("Animation", "Worlds brought to life through artistry and CGI.")),
            ("comedy", 4, ("Komedi", "Eğlenceli ve güldürü odaklı yapımlar."), ("Comedy", "Lighthearted, humorous, and entertaining stories.")),
            ("crime", 5, ("Suç", "Suç dünyası, dedektiflik ve adalet mücadeleleri."), ("Crime", "Underworld, investigative, and law enforcement narratives.")),
            ("documentary", 6, ("Belgesel", "Gerçek olayları, kişileri ve doğayı anlatan yapımlar."), ("Documentary", "Non-fictional insights into real events, people, and nature.")),
            ("drama", 7, ("Dram", "Duygusal derinlik ve karakter odaklı çatışmalar."), ("Drama", "In-depth emotional resonance and character conflict.")),
            ("family", 8, ("Aile", "Tüm ailenin keyifle izleyebileceği yapımlar."), ("Family", "Wholesome entertainment for all ages.")),
            ("fantasy", 9, ("Fantastik", "Büyülü dünyalar ve doğaüstü mitolojiler."), ("Fantasy", "Mythical realms, magic, and supernatural worlds.")),
            ("history", 10, ("Tarih", "Geçmiş dönemleri ve tarihi olayları konu alan yapımlar."), ("History", "Narratives depicting historical eras and milestones.")),
            ("horror", 11, ("Korku", "Gerilim, gizem ve korku unsurları içeren yapımlar."), ("Horror", "Macabre, terrifying, and fear-inducing storylines.")),
            ("music", 12, ("Müzik", "Müzik, müzikal performanslar ve sanatçı hikayeleri."), ("Music", "Musical journeys, performances, and melodic themes.")),
            ("mystery", 13, ("Gizem", "Sırların çözülmeyi beklediği merak uyandırıcı hikayeler."), ("Mystery", "Puzzling riddles, enigmas, and suspenseful investigations.")),
            ("romance", 14, ("Romantik", "Aşk, tutku ve ilişkiler üzerine kurulu hikayeler."), ("Romance", "Tales of love, passion, and human connection.")),
            ("science-fiction", 15, ("Bilim Kurgu", "Gelecek, uzay, teknoloji ve alternatif gerçeklikler."), ("Science Fiction", "Futuristic, speculative, and technologically driven narratives.")),
            ("tv-movie", 16, ("TV Filmi", "Televizyon yayını için özel olarak üretilen filmler."), ("TV Movie", "Feature films produced specifically for television.")),
            ("thriller", 17, ("Gerilim", "Soluk kesen tansiyon ve psikolojik gerilim."), ("Thriller", "High-stakes tension and suspenseful twists.")),
            ("war", 18, ("Savaş", "Savaşın askeri ve insani boyutlarını işleyen yapımlar."), ("War", "Depictions of military conflict and human survival.")),
            ("western", 19, ("Vahşi Batı", "Klasik kovboylar, kanunsuz topraklar ve batı hikayeleri."), ("Western", "Frontier life, outlaws, and classic gunslinger tales."))
        };

        foreach (var g in genresToSeed)
        {
            if (existingCodes.Contains(g.Code))
                continue;

            var genre = Genre.Create(g.Code, g.Order);
            genre.AddOrUpdateTranslation(LanguageCode.Turkish, g.Item3.TrName, g.Item3.TrDesc);
            genre.AddOrUpdateTranslation(LanguageCode.English, g.Item4.EnName, g.Item4.EnDesc);
            genre.ClearDomainEvents();

            context.Genres.Add(genre);
        }
    }

    private static async Task SeedSampleMediaAsync(CatalogDbContext context, CancellationToken cancellationToken)
    {
        var hasAnyMedia = await context.Media.IgnoreQueryFilters().AnyAsync(cancellationToken);
        if (hasAnyMedia)
            return;

        // Platform, Mood ve Genre referanslarını çekelim (Local ChangeTracker fallback ile)
        var netflix = context.Platforms.Local.FirstOrDefault(p => p.Slug == "netflix")
            ?? await context.Platforms.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Slug == "netflix", cancellationToken);
        var prime = context.Platforms.Local.FirstOrDefault(p => p.Slug == "prime-video")
            ?? await context.Platforms.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Slug == "prime-video", cancellationToken);
        var mindBending = context.Moods.Local.FirstOrDefault(m => m.Code == "beyin-yakanlar")
            ?? await context.Moods.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Code == "beyin-yakanlar", cancellationToken);
        var sciFi = context.Genres.Local.FirstOrDefault(g => g.Code == "science-fiction")
            ?? await context.Genres.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Code == "science-fiction", cancellationToken);
        var action = context.Genres.Local.FirstOrDefault(g => g.Code == "action")
            ?? await context.Genres.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Code == "action", cancellationToken);
        var drama = context.Genres.Local.FirstOrDefault(g => g.Code == "drama")
            ?? await context.Genres.IgnoreQueryFilters().FirstOrDefaultAsync(g => g.Code == "drama", cancellationToken);

        // 1. Örnek Film: Inception
        var inception = Movie.Create(
            releaseYear: 2010,
            runtimeMinutes: 148,
            posterUrl: "https://image.tmdb.org/t/p/original/edv5CZvWj09upOsy2Y6IwDhK8bt.jpg",
            backdropUrl: "https://image.tmdb.org/t/p/original/s3TBrRGB1iav7gFOCNx3H31MoES.jpg",
            imdbId: "tt1375666",
            imdbRating: 8.8,
            imdbVotes: 2500000,
            director: "Christopher Nolan");

        inception.AddOrUpdateTranslation(LanguageCode.Turkish, "Başlangıç",
            "Dom Cobb, zihnin en savunmasız olduğu rüya görme anında bilinçaltının derinliklerindeki değerli sırları çalan yetenekli bir hırsızdır.",
            "Rüya içinde rüya.");

        inception.AddOrUpdateTranslation(LanguageCode.English, "Inception",
            "A thief who steals corporate secrets through the use of dream-sharing technology is given the inverse task of planting an idea into the mind of a C.E.O.",
            "Your mind is the scene of the crime.");

        if (netflix is not null)
            inception.AddPlatform(netflix.Id, "https://www.netflix.com/title/70131314", "netflix://title/70131314");

        if (prime is not null)
            inception.AddPlatform(prime.Id, "https://www.primevideo.com/detail/0S8G59B1V1", "primevideo://detail/0S8G59B1V1");

        if (mindBending is not null)
            inception.AddMood(mindBending.Id);

        if (sciFi is not null)
            inception.AddGenre(sciFi.Id);

        if (action is not null)
            inception.AddGenre(action.Id);

        inception.ClearDomainEvents();
        context.Movies.Add(inception);

        // 2. Örnek Antoloji Dizisi: Black Mirror (Bölüm Ruleti ve Sadece Bu Diziyi Shuffle Yap için)
        var blackMirror = Series.Create(
            releaseYear: 2011,
            seriesType: SeriesType.EpisodicAnthology,
            posterUrl: "https://image.tmdb.org/t/p/original/5UaMirnkW6v99uqR5Bw9ZfB63jE.jpg",
            backdropUrl: "https://image.tmdb.org/t/p/original/vxuoP3c9Vq2P4v2f92r4r6y1A6T.jpg",
            imdbId: "tt2085059",
            imdbRating: 8.7,
            imdbVotes: 620000,
            totalSeasons: 6);

        blackMirror.AddOrUpdateTranslation(LanguageCode.Turkish, "Black Mirror",
            "İnsanlığın en büyük yeniliklerinin ve en karanlık içgüdülerinin çarpıştığı karmaşık, teknolojiyle dolu bir geleceğe odaklanan antoloji dizisi.",
            "Gelecek parlak ama aynası siyah.");

        blackMirror.AddOrUpdateTranslation(LanguageCode.English, "Black Mirror",
            "An anthology series exploring a twisted, high-tech multiverse where humanity's greatest innovations and darkest instincts collide.",
            "The future is bright, but the mirror is dark.");

        if (netflix is not null)
            blackMirror.AddPlatform(netflix.Id, "https://www.netflix.com/title/70264888", "netflix://title/70264888");

        if (mindBending is not null)
            blackMirror.AddMood(mindBending.Id);

        if (sciFi is not null)
            blackMirror.AddGenre(sciFi.Id);

        if (drama is not null)
            blackMirror.AddGenre(drama.Id);

        // Bağımsız bölümler (Bölüm Ruleti)
        var s1e1 = blackMirror.AddEpisode(1, 1, isStandalone: true, runtimeMinutes: 44, imdbRating: 7.7,
            thumbnailUrl: "https://image.tmdb.org/t/p/w500/bm1h9T4gZ8bK6v8f9s3T5r8g1A.jpg");
        s1e1.AddOrUpdateTranslation(LanguageCode.Turkish, "Milli Marş (The National Anthem)", "Başbakan Michael Callow, Kraliyet Ailesi'nin çok sevilen bir üyesi kaçırıldığında şok edici bir ikilemle karşı karşıya kalır.");
        s1e1.AddOrUpdateTranslation(LanguageCode.English, "The National Anthem", "Prime Minister Michael Callow faces a shocking dilemma when Princess Susannah is kidnapped.");
        if (netflix is not null) s1e1.AddPlatform(netflix.Id, "https://www.netflix.com/watch/70264888", "netflix://watch/70264888");

        var s1e2 = blackMirror.AddEpisode(1, 2, isStandalone: true, runtimeMinutes: 62, imdbRating: 8.0,
            thumbnailUrl: "https://image.tmdb.org/t/p/w500/15millionmerits.jpg");
        s1e2.AddOrUpdateTranslation(LanguageCode.Turkish, "15 Milyon Değer (15 Million Merits)", "Günlük hayatın egzersiz bisikletlerinde puan kazanmaktan ibaret olduğu bir dünyada iki genç özgürlük arar.");
        s1e2.AddOrUpdateTranslation(LanguageCode.English, "15 Million Merits", "In a world where daily life consists of riding exercise bikes for credits, two youths seek freedom.");
        if (netflix is not null) s1e2.AddPlatform(netflix.Id, "https://www.netflix.com/watch/70264889", "netflix://watch/70264889");

        var s3e1 = blackMirror.AddEpisode(3, 1, isStandalone: true, runtimeMinutes: 63, imdbRating: 8.3,
            thumbnailUrl: "https://image.tmdb.org/t/p/w500/nosedive.jpg");
        s3e1.AddOrUpdateTranslation(LanguageCode.Turkish, "Düşüş (Nosedive)", "İnsanların sosyal puanlarına göre sınıflandırıldığı bir toplumda, genç bir kadın statüsünü artırmak için her şeyi riske atar.");
        s3e1.AddOrUpdateTranslation(LanguageCode.English, "Nosedive", "A woman desperate to boost her socioeconomic standing goes to extremes at a friend's wedding.");
        if (netflix is not null) s3e1.AddPlatform(netflix.Id, "https://www.netflix.com/watch/80104625", "netflix://watch/80104625");

        var s3e4 = blackMirror.AddEpisode(3, 4, isStandalone: true, runtimeMinutes: 61, imdbRating: 8.6,
            thumbnailUrl: "https://image.tmdb.org/t/p/w500/sanjunipero.jpg");
        s3e4.AddOrUpdateTranslation(LanguageCode.Turkish, "San Junipero", "1987 yılında bir sahil kasabasında utangaç bir kadınla dışadönük bir parti kızı arasında beklenmedik bir aşk doğar.");
        s3e4.AddOrUpdateTranslation(LanguageCode.English, "San Junipero", "In a seaside town in 1987, a shy young woman and an outgoing girl strike up an unexpected bond.");
        if (netflix is not null) s3e4.AddPlatform(netflix.Id, "https://www.netflix.com/watch/80104628", "netflix://watch/80104628");

        blackMirror.ClearDomainEvents();
        context.Series.Add(blackMirror);
    }
}
