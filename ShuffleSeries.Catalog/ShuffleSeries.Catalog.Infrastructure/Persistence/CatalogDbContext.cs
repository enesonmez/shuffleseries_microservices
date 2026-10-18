using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Domain.Entities;
using ShuffleSeries.Shared.Core.Domain.Outbox;
using ShuffleSeries.Shared.Core.Domain.Repositories;
using ShuffleSeries.Shared.Core.Infrastructure.Extensions;

namespace ShuffleSeries.Catalog.Infrastructure.Persistence;

public sealed class CatalogDbContext : DbContext, IUnitOfWork
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options) { }

    public DbSet<Media> Media => Set<Media>();
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Series> Series => Set<Series>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<Platform> Platforms => Set<Platform>();
    public DbSet<Mood> Moods => Set<Mood>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<MediaTranslation> MediaTranslations => Set<MediaTranslation>();
    public DbSet<EpisodeTranslation> EpisodeTranslations => Set<EpisodeTranslation>();
    public DbSet<MoodTranslation> MoodTranslations => Set<MoodTranslation>();
    public DbSet<GenreTranslation> GenreTranslations => Set<GenreTranslation>();
    public DbSet<MediaPlatform> MediaPlatforms => Set<MediaPlatform>();
    public DbSet<MediaMood> MediaMoods => Set<MediaMood>();
    public DbSet<MediaGenre> MediaGenres => Set<MediaGenre>();
    public DbSet<EpisodePlatform> EpisodePlatforms => Set<EpisodePlatform>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
        modelBuilder.ApplySoftDeleteQueryFilters();
    }
}
