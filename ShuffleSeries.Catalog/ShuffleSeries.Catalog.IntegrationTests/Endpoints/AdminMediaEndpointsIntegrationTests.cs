using System.Net;
using System.Net.Http.Json;
using ShuffleSeries.Catalog.Api.Contracts.Requests.Movies;
using ShuffleSeries.Catalog.Application.Features.AdminMedia.Responses;
using ShuffleSeries.Catalog.IntegrationTests.Infrastructure;
using ShuffleSeries.Shared.Core.Application.Responses;
using ShuffleSeries.Shared.Core.Domain.Constants;

namespace ShuffleSeries.Catalog.IntegrationTests.Endpoints;

public class AdminMediaEndpointsIntegrationTests : CatalogIntegrationTestBase
{
    public AdminMediaEndpointsIntegrationTests(CatalogApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetAdminMediaList_WhenAuthorizedAsAdmin_ShouldReturnPagedResults()
    {
        // Arrange - Create a movie so we have guaranteed records
        var movieRequest = new CreateMovieRequest(
            TmdbId: 999901,
            ImdbId: "tt9999001",
            ReleaseYear: 2010,
            RuntimeMinutes: 148,
            PosterUrl: "https://example.com/inception.jpg",
            BackdropUrl: null,
            LanguageCode: "tr",
            Title: "Inception Admin Test",
            Overview: "Admin list test overview.");

        var postResponse = await Client.PostAsJsonAsync("api/catalog/movies", movieRequest);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Query as Admin
        var response = await Client.GetAsync("api/catalog/admin/media?page=1&pageSize=10&search=Inception");

        // Assert HTTP
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AdminMediaListItemResponse>>();

        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Items.Should().Contain(m => m.Title.Contains("Inception"));
        result.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetAdminMediaList_WhenUnauthorized_ShouldReturn401Unauthorized()
    {
        // Arrange
        var unauthorizedClient = CreateUnauthorizedClient();

        // Act
        var response = await unauthorizedClient.GetAsync("api/catalog/admin/media");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAdminMediaList_WhenNonAdminUser_ShouldReturn403Forbidden()
    {
        // Arrange - Non-admin user (e.g. Standard user with movies read permissions)
        var standardClient = CreateAuthorizedClient(
            role: SystemRoles.Standard,
            permissions: [SystemPermissions.Catalog.MoviesRead]);

        // Act
        var response = await standardClient.GetAsync("api/catalog/admin/media");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAdminMediaList_WhenNonAdminWithAdminMediaReadPermission_ShouldReturn200OK()
    {
        // Arrange - Standard user granted catalog:admin:read permission
        var cmsOperatorClient = CreateAuthorizedClient(
            role: SystemRoles.Standard,
            permissions: [SystemPermissions.Catalog.AdminMediaRead]);

        // Act
        var response = await cmsOperatorClient.GetAsync("api/catalog/admin/media?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAdminMediaList_WithIsDeletedFilter_ShouldReturnOnlySoftDeletedRecords()
    {
        // Arrange - Create a movie and delete it (soft-delete)
        var movieRequest = new CreateMovieRequest(
            TmdbId: Random.Shared.Next(100000, 999999),
            ImdbId: $"tt{Random.Shared.Next(1000000, 9999999)}",
            ReleaseYear: 2021,
            RuntimeMinutes: 110,
            PosterUrl: "https://example.com/archived.jpg",
            BackdropUrl: null,
            LanguageCode: "tr",
            Title: "Archived Soft Deleted Movie",
            Overview: "To be soft deleted.");

        var postResponse = await Client.PostAsJsonAsync("api/catalog/movies", movieRequest);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var movieId = await postResponse.Content.ReadFromJsonAsync<Guid>();

        // Soft delete the movie
        var deleteResponse = await Client.DeleteAsync($"api/catalog/movies/{movieId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act 1: Default query (isDeleted is not passed or false) -> should NOT contain deleted movie
        var activeResponse = await Client.GetAsync($"api/catalog/admin/media?search=Archived");
        var activeResult = await activeResponse.Content.ReadFromJsonAsync<PaginatedList<AdminMediaListItemResponse>>();
        activeResult!.Items.Should().NotContain(m => m.Id == movieId);

        // Act 2: Query with isDeleted=true -> SHOULD contain soft deleted movie with IsDeleted = true
        var archiveResponse = await Client.GetAsync($"api/catalog/admin/media?isDeleted=true&search=Archived");
        var archiveResult = await archiveResponse.Content.ReadFromJsonAsync<PaginatedList<AdminMediaListItemResponse>>();
        archiveResult!.Items.Should().Contain(m => m.Id == movieId && m.IsDeleted);
    }

    [Fact]
    public async Task GetAdminMediaList_WithSortBy_ShouldReturnOrderedResults()
    {
        // Arrange - Query with sortBy=imdbRating and sortDescending=true
        var response = await Client.GetAsync("api/catalog/admin/media?page=1&pageSize=10&sortBy=imdbRating&sortDescending=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PaginatedList<AdminMediaListItemResponse>>();
        result.Should().NotBeNull();
        if (result!.Items.Count > 1)
        {
            var ratings = result.Items.Where(x => x.ImdbRating.HasValue).Select(x => x.ImdbRating!.Value).ToList();
            ratings.Should().BeInDescendingOrder();
        }
    }
}
