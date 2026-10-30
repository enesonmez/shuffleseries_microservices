using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using ShuffleSeries.Catalog.Api.Contracts.Requests.Movies;
using ShuffleSeries.Catalog.Application.Features.Movies.Responses;
using ShuffleSeries.Catalog.IntegrationTests.Infrastructure;
using ShuffleSeries.Shared.Core.Domain.Constants;

namespace ShuffleSeries.Catalog.IntegrationTests.Endpoints;

public class MovieEndpointsIntegrationTests : CatalogIntegrationTestBase
{
    public MovieEndpointsIntegrationTests(CatalogApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateMovie_WithValidPayload_ShouldPersistInRealPostgreSql_AndReturn201Created()
    {
        // Arrange - Get seeded platform, mood and genre IDs
        Guid platformId = Guid.Empty;
        Guid moodId = Guid.Empty;
        Guid genreId = Guid.Empty;

        await ExecuteDbContextAsync(async context =>
        {
            platformId = (await context.Platforms.FirstAsync(p => p.Slug == "netflix")).Id;
            moodId = (await context.Moods.FirstAsync(m => m.Code == "beyin-yakanlar")).Id;
            genreId = (await context.Genres.FirstAsync(g => g.Code == "science-fiction")).Id;
        });

        var request = new CreateMovieRequest(
            TmdbId: 872585,
            ImdbId: "tt15398776",
            ReleaseYear: 2023,
            RuntimeMinutes: 180,
            PosterUrl: "https://example.com/oppenheimer.jpg",
            BackdropUrl: "https://example.com/oppenheimer-bg.jpg",
            LanguageCode: "tr",
            Title: "Oppenheimer",
            Overview: "Atom bombasının babası.",
            Tagline: "Dünyayı sonsuza dek değiştiren adam.",
            Director: "Christopher Nolan",
            ImdbRating: 8.9,
            ImdbVotes: 700000,
            PlatformIds: [platformId],
            MoodIds: [moodId],
            GenreIds: [genreId]);

        // Act
        var response = await Client.PostAsJsonAsync("api/catalog/movies", request);

        // Assert HTTP
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var movieId = await response.Content.ReadFromJsonAsync<Guid>();
        movieId.Should().NotBeEmpty();

        // Assert Database State
        await ExecuteDbContextAsync(async context =>
        {
            var movie = await context.Movies
                .Include(m => m.Translations)
                .Include(m => m.MediaPlatforms)
                .Include(m => m.MediaMoods)
                .Include(m => m.MediaGenres)
                .FirstOrDefaultAsync(m => m.Id == movieId);

            movie.Should().NotBeNull();
            movie!.Director.Should().Be("Christopher Nolan");
            movie.ReleaseYear.Should().Be(2023);
            movie.RuntimeMinutes.Should().Be(180);
            movie.ImdbId.Should().Be("tt15398776");

            movie.Translations.Should().ContainSingle(t => t.LanguageCode == "tr" && t.Title == "Oppenheimer");
            movie.MediaPlatforms.Should().ContainSingle(mp => mp.PlatformId == platformId);
            movie.MediaMoods.Should().ContainSingle(mm => mm.MoodId == moodId);
            movie.MediaGenres.Should().ContainSingle(mg => mg.GenreId == genreId);
        });
    }

    [Fact]
    public async Task CreateMovie_WithoutAuthorization_ShouldReturn401Unauthorized()
    {
        // Arrange
        var unauthorizedClient = CreateUnauthorizedClient();
        var request = new CreateMovieRequest(
            TmdbId: 438631,
            ImdbId: "tt1160419",
            ReleaseYear: 2021,
            RuntimeMinutes: 155,
            PosterUrl: "https://example.com/dune.jpg",
            BackdropUrl: null,
            LanguageCode: "tr",
            Title: "Dune",
            Overview: "Çöl gezegeni macerası.",
            Director: "Denis Villeneuve");

        // Act
        var response = await unauthorizedClient.PostAsJsonAsync("api/catalog/movies", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateMovie_WithMissingPermission_ShouldReturn403Forbidden()
    {
        // Arrange - Client with read permission only (no catalog:movies:create)
        var forbiddenClient = CreateAuthorizedClient(
            role: SystemRoles.Standard,
            permissions: [SystemPermissions.Catalog.MoviesRead]);

        var request = new CreateMovieRequest(
            TmdbId: 438631,
            ImdbId: "tt1160419",
            ReleaseYear: 2021,
            RuntimeMinutes: 155,
            PosterUrl: "https://example.com/dune.jpg",
            BackdropUrl: null,
            LanguageCode: "tr",
            Title: "Dune",
            Overview: "Çöl gezegeni macerası.",
            Director: "Denis Villeneuve");

        // Act
        var response = await forbiddenClient.PostAsJsonAsync("api/catalog/movies", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateMovie_WithDuplicateImdbId_ShouldReturn422UnprocessableEntity()
    {
        // Arrange
        var request1 = new CreateMovieRequest(
            TmdbId: 603,
            ImdbId: "tt0133093",
            ReleaseYear: 1999,
            RuntimeMinutes: 136,
            PosterUrl: "https://example.com/matrix1.jpg",
            BackdropUrl: null,
            LanguageCode: "tr",
            Title: "Matrix 1",
            Overview: "Simülasyon dünyası.");

        var request2 = new CreateMovieRequest(
            TmdbId: 604,
            ImdbId: "tt0133093", // Duplicate IMDb ID
            ReleaseYear: 2003,
            RuntimeMinutes: 138,
            PosterUrl: "https://example.com/matrix2.jpg",
            BackdropUrl: null,
            LanguageCode: "tr",
            Title: "Matrix 2",
            Overview: "Simülasyon dünyası 2.");

        // Act
        var firstResponse = await Client.PostAsJsonAsync("api/catalog/movies", request1);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondResponse = await Client.PostAsJsonAsync("api/catalog/movies", request2);

        // Assert
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetMovieById_WithContentNegotiation_ShouldReturnLocalizedData()
    {
        // Arrange - Create movie with tr translation and then add en translation
        var createRequest = new CreateMovieRequest(
            TmdbId: 157336,
            ImdbId: "tt0816692",
            ReleaseYear: 2014,
            RuntimeMinutes: 169,
            PosterUrl: "https://example.com/interstellar.jpg",
            BackdropUrl: null,
            LanguageCode: "tr",
            Title: "Yıldızlararası",
            Overview: "İnsanlığın yeni bir yuva arayışı.",
            Director: "Christopher Nolan",
            ImdbRating: 8.7,
            ImdbVotes: 1900000);

        var postResponse = await Client.PostAsJsonAsync("api/catalog/movies", createRequest);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var movieId = await postResponse.Content.ReadFromJsonAsync<Guid>();

        // Add English translation
        var addTranslationResponse = await Client.PostAsJsonAsync(
            $"api/catalog/media/{movieId}/translations",
            new { LanguageCode = "en", Title = "Interstellar", Overview = "Mankind's next adventure." });
        addTranslationResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act 1: Query with ?lang=en
        var enResponse = await Client.GetAsync($"api/catalog/movies/{movieId}?lang=en");
        enResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var enMovie = await enResponse.Content.ReadFromJsonAsync<MovieDetailResponse>();
        enMovie.Should().NotBeNull();
        enMovie!.Title.Should().Be("Interstellar");
        enMovie.Overview.Should().Be("Mankind's next adventure.");

        // Act 2: Query with Accept-Language: tr
        var trClient = CreateAuthorizedClient();
        trClient.DefaultRequestHeaders.AcceptLanguage.Clear();
        trClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("tr-TR"));
        var trResponse = await trClient.GetAsync($"api/catalog/movies/{movieId}");
        trResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var trMovie = await trResponse.Content.ReadFromJsonAsync<MovieDetailResponse>();
        trMovie.Should().NotBeNull();
        trMovie!.Title.Should().Be("Yıldızlararası");
    }

    [Fact]
    public async Task GetMovieById_WhenUnauthorized_ShouldReturn401Unauthorized()
    {
        // Arrange
        var unauthorizedClient = CreateUnauthorizedClient();

        // Act
        var response = await unauthorizedClient.GetAsync($"api/catalog/movies/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMovieById_WithMissingPermission_ShouldReturn403Forbidden()
    {
        // Arrange - Client with only shuffle permission, no catalog read permission
        var forbiddenClient = CreateAuthorizedClient(
            role: SystemRoles.Standard,
            permissions: [SystemPermissions.ShuffleBasic]);

        // Act
        var response = await forbiddenClient.GetAsync($"api/catalog/movies/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMovieById_WithGranularMoviesReadPermission_ShouldReturn200OK()
    {
        // Arrange - Create a movie with admin Client
        var createRequest = new CreateMovieRequest(
            ReleaseYear: 2020,
            RuntimeMinutes: 150,
            PosterUrl: "https://example.com/tenet.jpg",
            BackdropUrl: null,
            LanguageCode: "tr",
            Title: "Tenet",
            Overview: "Zaman akışı savaşı.",
            Director: "Christopher Nolan",
            TmdbId: 577922,
            ImdbId: "tt6723592");

        var postResponse = await Client.PostAsJsonAsync("api/catalog/movies", createRequest);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var movieId = await postResponse.Content.ReadFromJsonAsync<Guid>();

        // Act - Query with client that ONLY has granular catalog:movies:read
        var granularClient = CreateAuthorizedClient(
            role: SystemRoles.Standard,
            permissions: [SystemPermissions.Catalog.MoviesRead]);

        var response = await granularClient.GetAsync($"api/catalog/movies/{movieId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var movie = await response.Content.ReadFromJsonAsync<MovieDetailResponse>();
        movie.Should().NotBeNull();
        movie!.Title.Should().Be("Tenet");
        movie.TmdbId.Should().Be(577922);
        movie.ImdbId.Should().Be("tt6723592");
    }

    [Fact]
    public async Task UpdateMovie_WithValidPayload_ShouldUpdateInPostgreSql_AndReturn204NoContent()
    {
        // Arrange - Create movie
        var createRequest = new CreateMovieRequest(
            ReleaseYear: 2008,
            RuntimeMinutes: 152,
            PosterUrl: "https://example.com/darkknight.jpg",
            BackdropUrl: null,
            LanguageCode: "tr",
            Title: "Kara Şövalye",
            Overview: "Gotham'ın koruyucusu.",
            Director: "Christopher Nolan",
            TmdbId: 155,
            ImdbId: "tt0468569",
            ImdbRating: 9.0,
            ImdbVotes: 2700000);

        var postResponse = await Client.PostAsJsonAsync("api/catalog/movies", createRequest);
        var movieId = await postResponse.Content.ReadFromJsonAsync<Guid>();

        // Act - Update
        var updateRequest = new UpdateMovieRequest(
            ReleaseYear: 2008,
            RuntimeMinutes: 153,
            Director: "C. Nolan",
            TmdbId: 155,
            ImdbId: "tt0468569",
            ImdbRating: 9.1,
            ImdbVotes: 2800000);

        var updateResponse = await Client.PutAsJsonAsync($"api/catalog/movies/{movieId}", updateRequest);

        // Assert HTTP
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert DB
        await ExecuteDbContextAsync(async context =>
        {
            var movie = await context.Movies.FindAsync(movieId);
            movie.Should().NotBeNull();
            movie!.RuntimeMinutes.Should().Be(153);
            movie.Director.Should().Be("C. Nolan");
            movie.TmdbId.Should().Be(155);
            movie.ImdbId.Should().Be("tt0468569");
            movie.ImdbRating.Should().Be(9.1);
            movie.ImdbVotes.Should().Be(2800000);
        });
    }

    [Fact]
    public async Task DeleteMovie_ShouldSoftDeleteInPostgreSql_AndReturn204NoContent()
    {
        // Arrange - Create movie
        var createRequest = new CreateMovieRequest(
            ReleaseYear: 2000,
            RuntimeMinutes: 155,
            PosterUrl: "https://example.com/gladiator.jpg",
            BackdropUrl: null,
            LanguageCode: "tr",
            Title: "Gladyatör",
            Overview: "Roma generali.",
            TmdbId: 98,
            ImdbId: "tt0172495");

        var postResponse = await Client.PostAsJsonAsync("api/catalog/movies", createRequest);
        var movieId = await postResponse.Content.ReadFromJsonAsync<Guid>();

        // Act - Delete
        var deleteResponse = await Client.DeleteAsync($"api/catalog/movies/{movieId}");

        // Assert HTTP
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert GET returns 404
        var getResponse = await Client.GetAsync($"api/catalog/movies/{movieId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Assert Database State (Soft Deleted)
        await ExecuteDbContextAsync(async context =>
        {
            var movie = await context.Movies.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == movieId);
            movie.Should().NotBeNull();
            movie!.IsDeleted.Should().BeTrue();
            movie.DeletedAtUtc.Should().NotBeNull();
        });
    }
}
