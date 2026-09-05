namespace TravelGuideDbModels;

//ორ წერტილს შორის გამოთვლილი მანძილები: საჰაერო მანძილი, გზით მანძილი და გზისთვის საჭირო დრო.
//ორივე წერტილი Locations ცხრილის ჩანაწერია: საწყისი — საწყისი წერტილის (FromPoints) მდებარეობა, საბოლოო —
//ადგილის ლოკაცია (PlacesByLocations). ნავიგაციები არ აქვს — ჩანაწერები იდენტიფიკატორებით იქმნება და იძებნება
public sealed class RouteDistanceModel
{
    public int RouteDistanceId { get; init; }
    public int StartLocationId { get; init; }
    public int EndLocationId { get; init; }

    //მანძილები კილომეტრებშია; ამ სამ ველს set აქვს, რადგან ხელახალი გამოთვლისას მნიშვნელობები ადგილზე სწორდება
    public double AirDistance { get; set; }
    public double RoadDistance { get; set; }
    public TimeSpan RoadTime { get; set; }
}
