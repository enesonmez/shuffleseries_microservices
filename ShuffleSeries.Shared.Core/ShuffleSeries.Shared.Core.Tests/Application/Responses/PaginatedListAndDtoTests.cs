using System.Text.Json;
using ShuffleSeries.Shared.Core.Application.Requests;
using ShuffleSeries.Shared.Core.Application.Responses;

namespace ShuffleSeries.Shared.Core.Tests.Application.Responses;

public class PaginatedListAndDtoTests
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private record SampleDto(int Id, string Name);

    [Fact]
    public void PaginatedList_ShouldCalculatePaginationCorrectly()
    {
        var items = new List<SampleDto>
        {
            new(1, "Alpha"),
            new(2, "Beta")
        };

        var list = new PaginatedList<SampleDto>(items, 25, 2, 10);

        list.Items.Should().HaveCount(2);
        list.TotalCount.Should().Be(25);
        list.PageNumber.Should().Be(2);
        list.PageSize.Should().Be(10);
        list.TotalPages.Should().Be(3);
        list.HasPreviousPage.Should().BeTrue();
        list.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void PaginatedList_WhenFirstPage_ShouldNotHavePreviousPage()
    {
        var items = new List<SampleDto> { new(1, "Alpha") };
        var list = new PaginatedList<SampleDto>(items, 1, 1, 10);

        list.HasPreviousPage.Should().BeFalse();
        list.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void PaginatedList_ShouldSerializeAndDeserializeJsonCorrectly()
    {
        var items = new List<SampleDto>
        {
            new(1, "Item 1"),
            new(2, "Item 2")
        };

        var original = new PaginatedList<SampleDto>(items, 50, 2, 10);

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<PaginatedList<SampleDto>>(json, _jsonOptions);

        deserialized.Should().NotBeNull();
        deserialized!.TotalCount.Should().Be(50);
        deserialized.PageNumber.Should().Be(2);
        deserialized.PageSize.Should().Be(10);
        deserialized.TotalPages.Should().Be(5);
        deserialized.Items.Should().HaveCount(2);
        deserialized.Items.First().Name.Should().Be("Item 1");
    }

    [Theory]
    [InlineData(0, 0, 1, 10)]
    [InlineData(-5, -10, 1, 10)]
    [InlineData(2, 50, 2, 50)]
    [InlineData(3, 500, 3, 100)] // Clamped to 100
    public void PaginationRequest_ShouldNormalizeValues(int inPage, int inSize, int expectedPage, int expectedSize)
    {
        var request = new PaginationRequest(inPage, inSize);

        request.PageNumber.Should().Be(expectedPage);
        request.PageSize.Should().Be(expectedSize);
    }
}
