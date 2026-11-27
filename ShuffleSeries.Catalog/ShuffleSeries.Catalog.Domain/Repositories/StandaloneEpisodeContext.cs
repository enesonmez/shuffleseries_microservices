using ShuffleSeries.Catalog.Domain.Entities;

namespace ShuffleSeries.Catalog.Domain.Repositories;

/// <summary>
/// Bağımsız bir bölümün (Standalone Episode) Bölüm Ruleti için gerekli üst sezon ve dizi bağlamını taşır.
/// </summary>
public sealed record StandaloneEpisodeContext(
    Episode Episode,
    Season Season,
    Series Series);
