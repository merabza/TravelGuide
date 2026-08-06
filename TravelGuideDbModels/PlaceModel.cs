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
    public List<string> Categories { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public string? BestSeason { get; set; }
    public List<string> Distances { get; set; } = [];
    public int? DistanceFromTbilisiKm { get; set; }
    public string? Description { get; set; }
    public EState State { get; set; }
}
