namespace TravelGuideDbModels;

//გვერდებს შორის ბმულების გრაფის წიბო: FromUrlId გვერდზე მოიძებნა GotUrlId მისამართი
public sealed class UrlGraphNode
{
    public int UgnId { get; init; }
    public int FromUrlId { get; init; }
    public int GotUrlId { get; init; }
}
