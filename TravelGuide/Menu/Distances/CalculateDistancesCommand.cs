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
using DoTravelGuide.Models;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Distances;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class CalculateDistancesCommand : CliMenuCommand
{
    //ერთი წყვილის დამუშავების შედეგი
    private enum EPairResult
    {
        Saved,
        Updated,
        Unchanged,
        Failed
    }

    //OSRM-ის საჯარო სერვისს ზედიზედ მოთხოვნები შესვენებით უნდა გაეგზავნოს
    private static readonly TimeSpan RequestDelay = TimeSpan.FromMilliseconds(500);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IParametersManager _parametersManager;
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public CalculateDistancesCommand(IParametersManager parametersManager,
        ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory,
        IHttpClientFactory httpClientFactory) : base("Calculate Distances", EMenuAction.Reload)
    {
        _parametersManager = parametersManager;
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
        _httpClientFactory = httpClientFactory;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        MenuAction = EMenuAction.Reload;

        var parameters = (TravelGuideParameters)_parametersManager.Parameters;

        //პარამეტრებში არჩეული უნდა იყოს მიმდინარე ადგილმდებარეობის სახელი
        string? myCurrentPlaceName = parameters.MyCurrentPlaceName;
        if (string.IsNullOrEmpty(myCurrentPlaceName))
        {
            StShared.WriteErrorLine("My Current Place Name is not set in parameters", true);
            return false;
        }

        //ამ სახელით ჩემს ადგილმდებარეობებში უნდა მოიძებნოს შესაბამისი კოორდინატები
        if (!parameters.MyPlaces.TryGetValue(myCurrentPlaceName, out MyPlace? myPlace))
        {
            StShared.WriteErrorLine($"My Place with name {myCurrentPlaceName} not found in My Places", true);
            return false;
        }

        ITravelGuideRepository repository = _travelGuideRepositoryCreatorFactory.GetTravelGuideRepository();

        //არსებული ჩანაწერები წყვილის კოორდინატებით: უარყოფითი პასუხისას გამოსატოვებლად,
        //დადებითი პასუხისას — სისწორის შესამოწმებლად და ჩასასწორებლად
        Dictionary<(double StartLatitude, double StartLongitude, double EndLatitude, double EndLongitude),
            RouteDistanceModel> existingByPair = repository.GetAllRouteDistances().ToDictionary(k =>
            (k.StartLatitude, k.StartLongitude, k.EndLatitude, k.EndLongitude));

        //თუ ბაზაში უკვე დათვლილი მანძილებია, მომხმარებელი ირჩევს: ყველა წყვილი თავიდან გადაითვალოს
        //და საჭიროებისას ჩასწორდეს, თუ მხოლოდ ჯერ დაუთვლელები დაითვალოს
        var reCalculate = false;
        if (existingByPair.Count > 0)
        {
            reCalculate = Inputer.InputBool("Re-calculate already counted distances?", false, false);
        }

        //ბაზიდან იტვირთება Locations ცხრილის ყველა ჩანაწერი — მანძილი ყველა ლოკაციამდე ითვლება,
        //ადგილებთან ბმულების მიუხედავად
        List<LocationModel> locations = repository.GetAllLocations();
        if (locations.Count == 0)
        {
            Console.WriteLine("No Locations found");
            return true;
        }

        Console.WriteLine(string.Create(CultureInfo.InvariantCulture,
            $"Calculate Distances started: from {myCurrentPlaceName} ({myPlace.Latitude}, {myPlace.Longitude}) to {locations.Count} locations"));

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

            //ლოკაციები კოორდინატებით უნიკალურია და წყვილი განმეორებით ვერ შეგვხვდება;
            //უკვე დათვლილი წყვილი მხოლოდ დადებითი პასუხისას გადაითვლება
            bool pairExists = existingByPair.TryGetValue(
                (myPlace.Latitude, myPlace.Longitude, location.Latitude, location.Longitude),
                out RouteDistanceModel? existingRouteDistance);
            if (pairExists && !reCalculate)
            {
                skippedCount++;
                continue;
            }

            string progressPrefix = string.Create(CultureInfo.InvariantCulture,
                $"{index + 1}/{locations.Count} Location {location.LocationId} ({location.Latitude:F6}, {location.Longitude:F6})");

            EPairResult pairResult = CountAndPersistPair(repository, myPlace, location, existingRouteDistance,
                progressPrefix, cancellationToken);

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
    private EPairResult CountAndPersistPair(ITravelGuideRepository repository, MyPlace myPlace,
        LocationModel location, RouteDistanceModel? existingRouteDistance, string progressPrefix,
        CancellationToken cancellationToken)
    {
        double airDistanceKm = DistanceCounter.CountAirDistanceKm(myPlace.Latitude, myPlace.Longitude,
            location.Latitude, location.Longitude);

        (double DistanceKm, TimeSpan Duration)? roadRoute = DistanceCounter.TryGetRoadRoute(_httpClientFactory,
            myPlace.Latitude, myPlace.Longitude, location.Latitude, location.Longitude, cancellationToken);

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
            repository.AddRouteDistance(new RouteDistanceModel
            {
                StartLatitude = myPlace.Latitude,
                StartLongitude = myPlace.Longitude,
                EndLatitude = location.Latitude,
                EndLongitude = location.Longitude,
                AirDistance = airDistanceKm,
                RoadDistance = roadRoute.Value.DistanceKm,
                RoadTime = roadRoute.Value.Duration
            });

            //თითო წყვილი ცალკე ინახება, რომ შეწყვეტილმა გაშვებამ არაფერი დაკარგოს
            repository.SaveChanges();
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
        repository.SaveChanges();
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
}
