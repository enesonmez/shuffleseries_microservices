using ShuffleSeries.Shared.Core.Primitives;

namespace ShuffleSeries.Catalog.Domain.Entities;

public class Series : AggregateRoot
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public bool IsIndependentEpisodes { get; private set; }
    
    private Series() {}

    private Series(Guid id, string title, string description, bool isIndependentEpisodes) : base(id)
    {
        Title = title;
        Description = description;
        IsIndependentEpisodes = isIndependentEpisodes;
    }

    public static Series Create(string title, string description, bool isIndependentEpisodes)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be empty");
        
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty");

        var series = new Series(
            Guid.NewGuid(),
            title, 
            description, 
            isIndependentEpisodes
        );
        
        return series;
    }
}