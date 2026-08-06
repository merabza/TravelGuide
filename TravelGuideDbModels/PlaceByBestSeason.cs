namespace TravelGuideDbModels;

public sealed class PlaceByBestSeason
{
    public int PlaceId { get; init; }
    public int MonthId { get; init; }

    public PlaceModel PlaceNavigation
    {
        get => field ?? throw new InvalidOperationException("Uninitialized property: " + nameof(PlaceNavigation));
        init;
    }

    public MonthModel MonthNavigation
    {
        get => field ?? throw new InvalidOperationException("Uninitialized property: " + nameof(MonthNavigation));
        init;
    }
}
