using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShuffleSeries.Catalog.Domain.Repositories;
using ShuffleSeries.Catalog.IntegrationTests.Infrastructure;
using ShuffleSeries.Shared.Core.Domain.ValueObjects;

namespace ShuffleSeries.Catalog.IntegrationTests.Endpoints;

public class CatalogSeedingAndGenreIntegrationTests : CatalogIntegrationTestBase
{
    public CatalogSeedingAndGenreIntegrationTests(CatalogApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task SeedData_ShouldContainAll19StandardGenres_WithTurkishAndEnglishTranslations()
    {
        await ExecuteDbContextAsync(async context =>
        {
            var genres = await context.Genres
                .Include(g => g.Translations)
                .ToListAsync();

            genres.Should().HaveCount(19);

            var sciFi = genres.FirstOrDefault(g => g.Code == "science-fiction");
            sciFi.Should().NotBeNull();
            sciFi!.Translations.Should().HaveCount(2);

            var tr = sciFi.Translations.FirstOrDefault(t => t.LanguageCode == LanguageCode.Turkish);
            tr.Should().NotBeNull();
            tr!.Name.Should().Be("Bilim Kurgu");

            var en = sciFi.Translations.FirstOrDefault(t => t.LanguageCode == LanguageCode.English);
            en.Should().NotBeNull();
            en!.Name.Should().Be("Science Fiction");
        });
    }

    [Fact]
    public async Task SeedData_SampleMedia_ShouldHaveGenresAssociated()
    {
        await ExecuteDbContextAsync(async context =>
        {
            var inception = await context.Movies
                .Include(m => m.MediaGenres)
                .ThenInclude(mg => mg.Genre)
                .FirstOrDefaultAsync(m => m.ImdbId == "tt1375666");

            inception.Should().NotBeNull();
            inception!.MediaGenres.Should().HaveCount(2);

            var blackMirror = await context.Series
                .Include(s => s.MediaGenres)
                .ThenInclude(mg => mg.Genre)
                .FirstOrDefaultAsync(s => s.ImdbId == "tt2085059");

            blackMirror.Should().NotBeNull();
            blackMirror!.MediaGenres.Should().HaveCount(2);
        });
    }

    [Fact]
    public async Task GenreRepository_ShouldResolveAndQueryActiveGenres()
    {
        using var scope = Factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGenreRepository>();

        var activeGenres = await repository.GetActiveGenresWithTranslationsAsync();
        activeGenres.Should().HaveCount(19);

        var actionGenre = await repository.GetByCodeAsync("action");
        actionGenre.Should().NotBeNull();
        actionGenre!.Translations.Should().Contain(t => t.LanguageCode == LanguageCode.Turkish && t.Name == "Aksiyon");
    }
}
