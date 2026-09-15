using TravelGuideCore.Domain.LocationModels;

namespace TravelGuide.Menu.Lists;

//საწყისი წერტილების ცნობარის რედაქტორის ჩანაწერი — სახელს მდებარეობა ემატება. Location ბმული ლოკაციის
//კოორდინატების ასლია ან null (ქროულერის შექმნილ წერტილებს მდებარეობა არ აქვთ); მდებარეობის რედაქტორი მას
//მთლიანად ცვლის, შენახვისას კი მისით Locations ცხრილის საზიარო ჩანაწერი მოიძებნება ან იქმნება
public sealed class FromPointItem : LookupItem
{
    public FromPointItem(int id, string name, LocationModel? location) : base(id, name)
    {
        Location = location is null ? null : new LocationItem(location);
    }

    public LocationItem? Location { get; set; }
}
