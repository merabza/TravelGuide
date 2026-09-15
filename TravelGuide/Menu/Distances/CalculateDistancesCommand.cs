using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using SystemTools.SystemToolsShared;
using TravelGuideCore.Domain.LocationModels;
using TravelGuideCore.Domain.RouteDistanceModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Distances;

//საწყისი წერტილის (FromPoints ცნობარის ჩანაწერის) მდებარეობიდან ადგილებთან მიბმულ ყველა ლოკაციამდე მანძილების
//გამოთვლა და RouteDistances ცხრილში შენახვა — საწყისი წერტილის რედაქტორის ჩანაწერის მენიუს პუნქტი. მარშრუტი
//საწყისი წერტილის ლოკაციიდან (FromPoints.LocationId) ადგილის ლოკაციამდე (PlacesByLocations.LocationId) ინახება
public sealed class CalculateDistancesCommand : CliMenuCommand
{
    //OSRM-ის საჯარო სერვისს ზედიზედ მოთხოვნები შესვენებით უნდა გაეგზავნოს
    private static readonly TimeSpan RequestDelay = TimeSpan.FromMilliseconds(500);

    private readonly string _fromPointName;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LocationModel? _startLocation;
    private readonly ITravelGuideRepository _travelGuideRepository;

    //startLocation საწყისი წერტილის მდებარეობაა ბაზაში არსებული იდენტიფიკატორით; მდებარეობის გარეშე წერტილისთვის
    //null — ბრძანება მაშინ შეცდომას წერს და არაფერს ითვლის
    public CalculateDistancesCommand(ITravelGuideRepository travelGuideRepository, IHttpClientFactory httpClientFactory,
        string fromPointName, LocationModel? startLocation) : base("Calculate Distances", EMenuAction.Reload)
    {
        _travelGuideRepository = travelGuideRepository;
        _httpClientFactory = httpClientFactory;
        _fromPointName = fromPointName;
        _startLocation = startLocation;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        MenuAction = EMenuAction.Reload;

        //მანძილები საწყისი წერტილის მდებარეობიდან ითვლება — ის ჯერ Location ველით უნდა შეივსოს
        if (_startLocation is not { } startLocation)
        {
            StShared.WriteErrorLine($"FromPoint {_fromPointName} has no Location", true);
            return false;
        }

        //ამ საწყისი ლოკაციიდან უკვე დათვლილი მარშრუტები საბოლოო ლოკაციის იდენტიფიკატორით: უარყოფითი პასუხისას
        //გამოსატოვებლად, დადებითი პასუხისას — სისწორის შესამოწმებლად და ჩასასწორებლად
        Dictionary<int, RouteDistanceModel> existingByEndLocationId = _travelGuideRepository
            .GetRouteDistancesByStartLocationId(startLocation.LocationId).ToDictionary(k => k.EndLocationId);

        //თუ ბაზაში უკვე დათვლილი მანძილებია, მომხმარებელი ირჩევს: ყველა წყვილი თავიდან გადაითვალოს
        //და საჭიროებისას ჩასწორდეს, თუ მხოლოდ ჯერ დაუთვლელები დაითვალოს
        var reCalculate = false;
        if (existingByEndLocationId.Count > 0)
        {
            reCalculate = Inputer.InputBool("Re-calculate already counted distances?", false, false);
        }

        //ბაზიდან იტვირთება ადგილებთან მიბმული ლოკაციები (PlacesByLocations) — თითო ლოკაცია ერთხელ, რამდენი
        //ადგილიც არ უნდა ეზიარებოდეს
        List<LocationModel> locations = _travelGuideRepository.GetPlaceLinkedLocations();
        if (locations.Count == 0)
        {
            Console.WriteLine("No Locations found");
            return true;
        }

        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"Calculate Distances started: from {_fromPointName} ({startLocation.Latitude}, {startLocation.Longitude}) to {locations.Count} locations"));

        var savedCount = 0;
        var updatedCount = 0;
        var unchangedCount = 0;
        var skippedCount = 0;
        var failedCount = 0;

        for (var index = 0; index < locations.Count; index++)
        {
            //მომხმარებლის მიერ შეწყვეტისას პროცესი მშვიდად ჩერდება — შენახული წყვილები ბაზაში რჩება
            if (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("Calculate Distances stopped");
                break;
            }

            LocationModel location = locations[index];

            //სიაში ლოკაცია არ მეორდება და წყვილი განმეორებით ვერ შეგვხვდება;
            //უკვე დათვლილი წყვილი მხოლოდ დადებითი პასუხისას გადაითვლება
            bool pairExists = existingByEndLocationId.TryGetValue(location.LocationId,
                out RouteDistanceModel? existingRouteDistance);
            if (pairExists && !reCalculate)
            {
                skippedCount++;
                continue;
            }

            string progressPrefix = string.Create(CultureInfo.InvariantCulture,
                $"{index + 1}/{locations.Count} Location {location.LocationId} ({location.Latitude:F6}, {location.Longitude:F6})");

            EPairResult pairResult = CountAndPersistPair(startLocation, location, existingRouteDistance, progressPrefix,
                cancellationToken);

            switch (pairResult)
            {
                case EPairResult.Saved:
                    savedCount++;
                    break;
                case EPairResult.Updated:
                    updatedCount++;
                    break;
                case EPairResult.Unchanged:
                    unchangedCount++;
                    break;
                case EPairResult.Failed:
                    failedCount++;
                    break;
                default:
                    throw new SwitchExpressionException();
            }

            //შეწყვეტისას გამონაკლისი აქ საჭირო არ არის — ციკლის დასაწყისი შეწყვეტას თავად ამოწმებს
            await Task.Delay(RequestDelay, cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }

        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"Calculate Distances finished: {savedCount} saved, {updatedCount} updated, {unchangedCount} unchanged, {skippedCount} skipped, {failedCount} failed"));
        return true;
    }

    //ერთი წყვილის დამუშავება: მანძილების გამოთვლა და შედეგის ბაზაში შენახვა, ან არსებულის შემოწმება-ჩასწორება
    private EPairResult CountAndPersistPair(LocationModel startLocation, LocationModel location,
        RouteDistanceModel? existingRouteDistance, string progressPrefix, CancellationToken cancellationToken)
    {
        double airDistanceKm = DistanceCounter.CountAirDistanceKm(startLocation.Latitude, startLocation.Longitude,
            location.Latitude, location.Longitude);

        (double DistanceKm, TimeSpan Duration)? roadRoute = DistanceCounter.TryGetRoadRoute(_httpClientFactory,
            startLocation.Latitude, startLocation.Longitude, location.Latitude, location.Longitude, cancellationToken);

        //გზის მარშრუტი ვერ დადგინდა — წყვილი არ ინახება და შემდეგი გაშვება მას ხელახლა ცდის
        if (roadRoute is null)
        {
            StShared.WriteErrorLine($"{progressPrefix}: road route request failed", true);
            return EPairResult.Failed;
        }

        string countedText = string.Create(CultureInfo.InvariantCulture,
            $"{progressPrefix}: air {airDistanceKm:F1}კმ, road {roadRoute.Value.DistanceKm:F1}კმ, {DistanceCounter.FormatDurationText(roadRoute.Value.Duration)}");

        if (existingRouteDistance is null)
        {
            _travelGuideRepository.AddRouteDistance(new RouteDistanceModel
            {
                StartLocationId = startLocation.LocationId,
                EndLocationId = location.LocationId,
                AirDistance = airDistanceKm,
                RoadDistance = roadRoute.Value.DistanceKm,
                RoadTime = roadRoute.Value.Duration
            });

            //თითო წყვილი ცალკე ინახება, რომ შეწყვეტილმა გაშვებამ არაფერი დაკარგოს
            _travelGuideRepository.SaveChanges();
            Console.WriteLine($"{countedText} (saved)");
            return EPairResult.Saved;
        }

        //არსებული ჩანაწერი სწორი აღმოჩნდა და ცვლილება არ სჭირდება
        if (!NeedsCorrection(existingRouteDistance, airDistanceKm, roadRoute.Value))
        {
            Console.WriteLine($"{countedText} (no change)");
            return EPairResult.Unchanged;
        }

        //არსებული ჩანაწერი ახლად გამოთვლილს აღარ ემთხვევა — ახალი მნიშვნელობებით სწორდება
        existingRouteDistance.AirDistance = airDistanceKm;
        existingRouteDistance.RoadDistance = roadRoute.Value.DistanceKm;
        existingRouteDistance.RoadTime = roadRoute.Value.Duration;
        _travelGuideRepository.SaveChanges();
        Console.WriteLine($"{countedText} (updated)");
        return EPairResult.Updated;
    }

    //ჩანაწერი ჩასასწორებელია, თუ რომელიმე მნიშვნელობა ახლად გამოთვლილს აღარ ემთხვევა.
    //მანძილები მცირე დაშვებით დარდება, რომ წილადის წარმოდგენის უმნიშვნელო სხვაობამ ჩასწორება არ გამოიწვიოს
    private static bool NeedsCorrection(RouteDistanceModel routeDistance, double airDistanceKm,
        (double DistanceKm, TimeSpan Duration) roadRoute)
    {
        const double toleranceKm = 0.0001;
        return Math.Abs(routeDistance.AirDistance - airDistanceKm) > toleranceKm ||
               Math.Abs(routeDistance.RoadDistance - roadRoute.DistanceKm) > toleranceKm ||
               routeDistance.RoadTime != roadRoute.Duration;
    }

    //ერთი წყვილის დამუშავების შედეგი
    private enum EPairResult
    {
        Saved,
        Updated,
        Unchanged,
        Failed
    }
}
