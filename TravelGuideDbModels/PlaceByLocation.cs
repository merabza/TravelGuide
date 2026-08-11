namespace TravelGuideDbModels;

public sealed class PlaceByLocation
{
    public int PlaceId { get; init; }
    public int LocationId { get; init; }

    public PlaceModel PlaceNavigation
    {
        get => field ?? throw new InvalidOperationException("Uninitialized property: " + nameof(PlaceNavigation));
        init;
    }

    public LocationModel LocationNavigation
    {
        get => field ?? throw new InvalidOperationException("Uninitialized property: " + nameof(LocationNavigation));
        init;
    }
}
