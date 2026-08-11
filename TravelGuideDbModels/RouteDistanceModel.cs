namespace TravelGuideDbModels;

//ორ წერტილს შორის გამოთვლილი მანძილები: საჰაერო მანძილი, გზით მანძილი და გზისთვის საჭირო დრო
public sealed class RouteDistanceModel
{
    public int RouteDistanceId { get; init; }
    public double StartLatitude { get; init; }
    public double StartLongitude { get; init; }
    public double EndLatitude { get; init; }
    public double EndLongitude { get; init; }

    //მანძილები კილომეტრებშია; ამ სამ ველს set აქვს, რადგან ხელახალი გამოთვლისას მნიშვნელობები ადგილზე სწორდება
    public double AirDistance { get; set; }
    public double RoadDistance { get; set; }
    public TimeSpan RoadTime { get; set; }
}
