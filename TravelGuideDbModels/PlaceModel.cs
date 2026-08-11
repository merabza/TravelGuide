// ReSharper disable CollectionNeverUpdated.Global

namespace TravelGuideDbModels;

public sealed class PlaceModel
{
    public int PlaceId { get; init; }
    public required string Url { get; init; }

    public string? Name { get; set; }

    public int? RegionId { get; set; }
    public int? MunicipalityId { get; set; }
    public string? Description { get; set; }
    public EState State { get; set; }

    public RegionModel? RegionNavigation { get; set; }
    public MunicipalityModel? MunicipalityNavigation { get; set; }

    public ICollection<PlaceByBestSeason> BestSeasons { get; init; } = new HashSet<PlaceByBestSeason>();
    public ICollection<PlaceByCategory> Categories { get; init; } = new HashSet<PlaceByCategory>();
    public ICollection<PlaceByTag> Tags { get; init; } = new HashSet<PlaceByTag>();
    public ICollection<DistanceByPlace> Distances { get; init; } = new HashSet<DistanceByPlace>();
    public ICollection<PlaceByLocation> Locations { get; init; } = new HashSet<PlaceByLocation>();
}
