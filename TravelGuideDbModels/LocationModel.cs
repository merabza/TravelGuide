namespace TravelGuideDbModels;

public sealed class LocationModel
{
    public int LocationId { get; init; }

    //კოორდინატები double ტიპისაა და არა decimal, რადგან DatabaseEntitiesDefaultConvention decimal-ს money სვეტად აქცევს
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}
