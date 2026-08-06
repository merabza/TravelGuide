namespace TravelGuideDbModels;

public sealed class PlaceByCategory
{
    public int PlaceId { get; init; }
    public int CategoryId { get; init; }

    public PlaceModel PlaceNavigation
    {
        get => field ?? throw new InvalidOperationException("Uninitialized property: " + nameof(PlaceNavigation));
        init;
    }

    public CategoryModel CategoryNavigation
    {
        get => field ?? throw new InvalidOperationException("Uninitialized property: " + nameof(CategoryNavigation));
        init;
    }
}
