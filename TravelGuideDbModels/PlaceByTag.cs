namespace TravelGuideDbModels;

public sealed class PlaceByTag
{
    public int PlaceId { get; init; }
    public int TagId { get; init; }

    public PlaceModel PlaceNavigation
    {
        get => field ?? throw new InvalidOperationException("Uninitialized property: " + nameof(PlaceNavigation));
        init;
    }

    public TagModel TagNavigation
    {
        get => field ?? throw new InvalidOperationException("Uninitialized property: " + nameof(TagNavigation));
        init;
    }
}
