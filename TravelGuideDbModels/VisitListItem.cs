namespace TravelGuideDbModels;

//ბოლო ვიზიტების სიის ელემენტი — ეკრანზე გამოსატანად აწყობილი მონაცემები, ბაზის ცხრილი არ არის.
//ვიზიტი ლოკაციაზეა: ადგილის სახელი ლოკაციაზე მიბმული ადგილიდან მოდის და null-ია, როცა ლოკაცია
//არცერთ ადგილს აღარ ებმება (ბმული მოიხსნა ან ადგილი წაიშალა) — კოორდინატები მაშინაც ჩანს
public sealed class VisitListItem
{
    public DateTime VisitDate { get; init; }
    public string? PlaceName { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public required string MotorcycleKey { get; init; }
}
