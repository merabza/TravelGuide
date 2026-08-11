namespace TravelGuideDbModels;

//ბოლო ვიზიტების სიის ელემენტი — ეკრანზე გამოსატანად აწყობილი მონაცემები, ბაზის ცხრილი არ არის
public sealed class VisitListItem
{
    public DateTime VisitDate { get; init; }
    public required string PlaceName { get; init; }
    public required string MotorcycleKey { get; init; }
}
