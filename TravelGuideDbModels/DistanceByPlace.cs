namespace TravelGuideDbModels;

public sealed class DistanceByPlace
{
    public int DistanceByPlaceId { get; init; }
    public int PlaceId { get; init; }
    public int FromPointId { get; init; }

    //ხელახალი ანალიზისას მანძილი შეიძლება შეიცვალოს, ამიტომ set-ია და არა init
    public int Distance { get; set; }

    public PlaceModel PlaceNavigation
    {
        get => field ?? throw new InvalidOperationException("Uninitialized property: " + nameof(PlaceNavigation));
        init;
    }

    public FromPointModel FromPointNavigation
    {
        get => field ?? throw new InvalidOperationException("Uninitialized property: " + nameof(FromPointNavigation));
        init;
    }
}
