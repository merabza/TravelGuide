namespace TravelGuideDbModels;

//ვიზიტზე მიბმული სურათის ფაილი. ინახება მხოლოდ ფაილის სახელი —
//ფოლდერის გზა პარამეტრებშია (TravelGuideParameters.ImagesFolderPath)
public sealed class VisitImage
{
    public int VisitImageId { get; init; }
    public int VisitId { get; init; }
    public required string FileName { get; set; }

    public VisitModel VisitNavigation
    {
        get => field ?? throw new InvalidOperationException("Uninitialized property: " + nameof(VisitNavigation));
        init;
    }
}
