using System.Globalization;
using SystemTools.SystemToolsShared;

namespace DoTravelGuide.Models;

public sealed class MyPlace : ItemData
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public override string GetItemKey()
    {
        return string.Create(CultureInfo.InvariantCulture, $"{Latitude}, {Longitude}");
    }
}
