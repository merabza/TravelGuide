using System.Globalization;
using SystemTools.SystemToolsShared;
using TravelGuideDbModels;

namespace TravelGuide.Menu.Lists;

//ადგილის ლოკაციების რედაქტორის ჩანაწერი — მიბმული ლოკაციის კოორდინატების მოუბმელი ასლი, რომელსაც
//ველების რედაქტორები ცვლიან. LocationId ის ლოკაციაა, რომელიც ჩანაწერის ჩატვირთვისას იყო მიბმული —
//შენახვისას ძველი ბმული ამით მოიძებნება (კოორდინატები კი შეიძლება უკვე შეცვლილი იყოს)
public sealed class LocationItem : ItemData
{
    public LocationItem()
    {
    }

    public LocationItem(LocationModel location)
    {
        LocationId = location.LocationId;
        Latitude = location.Latitude;
        Longitude = location.Longitude;
    }

    public int LocationId { get; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    //გასაღები კოორდინატებია მძიმით — ისე, როგორც Recommended Visits-ში, რომ Google Maps-ის ძებნაში
    //პირდაპირ ჩაკოპირება შეიძლებოდეს
    public override string GetItemKey()
    {
        return string.Create(CultureInfo.InvariantCulture, $"{Latitude}, {Longitude}");
    }
}
