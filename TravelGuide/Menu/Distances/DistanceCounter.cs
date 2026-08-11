using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;

namespace TravelGuide.Menu.Distances;

//ორ გეოგრაფიულ წერტილს შორის მანძილების გამოთვლის საერთო ლოგიკა
public static class DistanceCounter
{
    //ჰავერსინუსის ფორმულა: სფეროზე ორ წერტილს შორის უმოკლესი (საჰაერო) მანძილი კილომეტრებში
    public static double CountAirDistanceKm(double fromLatitude, double fromLongitude, double toLatitude,
        double toLongitude)
    {
        const double earthRadiusKm = 6371.0;
        double latitudeRadians = (toLatitude - fromLatitude) * Math.PI / 180;
        double longitudeRadians = (toLongitude - fromLongitude) * Math.PI / 180;
        double a = Math.Sin(latitudeRadians / 2) * Math.Sin(latitudeRadians / 2) +
                   Math.Cos(fromLatitude * Math.PI / 180) * Math.Cos(toLatitude * Math.PI / 180) *
                   Math.Sin(longitudeRadians / 2) * Math.Sin(longitudeRadians / 2);
        return 2 * earthRadiusKm * Math.Asin(Math.Sqrt(a));
    }

    //OSRM-ის საჯარო სერვისით გზის მანძილისა და დროის დადგენა ავტომობილის რეჟიმში.
    //წარუმატებლობისას გამონაკლისის მაგივრად null ბრუნდება, რომ გამომძახებელმა მუშაობა შეძლოს გააგრძელოს
    public static (double DistanceKm, TimeSpan Duration)? TryGetRoadRoute(IHttpClientFactory httpClientFactory,
        double fromLatitude, double fromLongitude, double toLatitude, double toLongitude,
        CancellationToken cancellationToken = default)
    {
        try
        {
            //OSRM-ს კოორდინატები გრძედი,განედი თანმიმდევრობით სჭირდება
            var url = new Uri(string.Create(CultureInfo.InvariantCulture,
                $"https://router.project-osrm.org/route/v1/driving/{fromLongitude},{fromLatitude};{toLongitude},{toLatitude}?overview=false"));

            // ReSharper disable once using
            using HttpClient httpClient = httpClientFactory.CreateClient();
            // ReSharper disable once using
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("TravelGuideBot", "1.0"));

            //საჯარო სერვისმა შეიძლება არ უპასუხოს — ლოდინი 10 წამით იზღუდება
            // ReSharper disable once using
            using CancellationTokenSource cancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));
            // ReSharper disable once using
            using HttpResponseMessage response = httpClient.Send(request, cancellationTokenSource.Token);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            // ReSharper disable once using
            using JsonDocument jsonDocument =
                JsonDocument.Parse(response.Content.ReadAsStream(cancellationTokenSource.Token));
            JsonElement root = jsonDocument.RootElement;
            if (root.GetProperty("code").GetString() != "Ok")
            {
                return null;
            }

            JsonElement route = root.GetProperty("routes")[0];
            const double metersInKilometer = 1000;
            double distanceKm = route.GetProperty("distance").GetDouble() / metersInKilometer;
            var duration = TimeSpan.FromSeconds(route.GetProperty("duration").GetDouble());
            return (distanceKm, duration);
        }
        catch (Exception)
        {
            //ინტერნეტის ან სერვისის პრობლემისას null ბრუნდება და გამომძახებელი გზის მონაცემების გარეშე აგრძელებს
            return null;
        }
    }

    //ხანგრძლივობის ტექსტი: ერთ საათზე მეტისთვის საათებით და წუთებით, თორემ მხოლოდ წუთებით
    public static string FormatDurationText(TimeSpan duration)
    {
        return duration.TotalHours >= 1
            ? string.Create(CultureInfo.InvariantCulture, $"{(int)duration.TotalHours}სთ {duration.Minutes}წთ")
            : string.Create(CultureInfo.InvariantCulture, $"{duration.Minutes}წთ");
    }
}
