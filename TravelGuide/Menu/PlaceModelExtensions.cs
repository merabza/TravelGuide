using System.Globalization;
using TravelGuideCore.Domain.PlaceModels;

namespace TravelGuide.Menu;

public static class PlaceModelExtensions
{
    //ადგილის წარწერა მენიუებში: დასახელება, უსახელოსთვის მისამართი, ხოლო არც-მისამართიანს (ხელით შეყვანილს,
    //რომელსაც სახელი არ მიეწერა) იდენტიფიკატორი წარმოადგენს — მისამართი (UrlNavigation, ჩატვირთული უნდა იყოს)
    //არასავალდებულოა და ორივე ერთდროულად ცარიელი შეიძლება იყოს
    public static string GetCaption(this PlaceModel place)
    {
        if (!string.IsNullOrWhiteSpace(place.Name))
        {
            return place.Name;
        }

        return place.UrlNavigation?.Url ?? string.Create(CultureInfo.InvariantCulture, $"Place {place.PlaceId}");
    }
}
