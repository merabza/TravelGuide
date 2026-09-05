namespace TravelGuideDbModels;

public sealed class FromPointModel
{
    public int FromPointId { get; init; }
    public required string Name { get; set; }

    //წერტილის მდებარეობა — Locations ცხრილის საზიარო ჩანაწერი. არასავალდებულოა: ქროულერი წერტილს მხოლოდ
    //სახელით ქმნის, კოორდინატები FromPoints ცნობარის რედაქტორით ივსება
    public int? LocationId { get; set; }
    public LocationModel? LocationNavigation { get; set; }
}
