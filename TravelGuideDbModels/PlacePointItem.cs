namespace TravelGuideDbModels;

//ადგილის კოორდინატები მანძილების გამოსათვლელად — ეკრანზე გამოსატანი სახელით, ბაზის ცხრილი არ არის
public sealed class PlacePointItem
{
    public required string PlaceName { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}
