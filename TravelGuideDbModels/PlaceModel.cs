// ReSharper disable CollectionNeverUpdated.Global

namespace TravelGuideDbModels;

public sealed class PlaceModel
{
    public int PlaceId { get; init; }
    public required string Url { get; init; }

    public string? Name { get; set; }

    //კოორდინატები double ტიპისაა და არა decimal, რადგან DatabaseEntitiesDefaultConvention decimal-ს money სვეტად აქცევს
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public string? Region { get; set; }
    public string? Municipality { get; set; }
    public string? Description { get; set; }
    public EState State { get; set; }

    public ICollection<PlaceByBestSeason> BestSeasons { get; init; } = new HashSet<PlaceByBestSeason>();
    public ICollection<PlaceByCategory> Categories { get; init; } = new HashSet<PlaceByCategory>();
    public ICollection<PlaceByTag> Tags { get; init; } = new HashSet<PlaceByTag>();
    public ICollection<DistanceByPlace> Distances { get; init; } = new HashSet<DistanceByPlace>();
}
