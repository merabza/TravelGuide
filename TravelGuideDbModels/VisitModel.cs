namespace TravelGuideDbModels;

//ვიზიტის ჩანაწერი: რომელი ადგილი, რომელი მოტოციკლით და რომელ თარიღში
public sealed class VisitModel
{
    public int VisitId { get; init; }
    public int PlaceId { get; init; }
    public int MotorcycleId { get; init; }
    public DateTime VisitDate { get; init; }
}
