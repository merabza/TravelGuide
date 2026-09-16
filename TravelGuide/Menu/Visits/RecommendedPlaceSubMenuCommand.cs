using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;
using TravelGuide.Menu.Distances;
using TravelGuideCore.Domain.LocationModels;
using TravelGuideCore.Domain.PlaceModels;
using TravelGuideCore.Domain.RouteDistanceModels;
using TravelGuideCore.Domain.VisitModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Visits;

public sealed class RecommendedPlaceSubMenuCommand : CliMenuCommand
{
    private readonly string _directionsUrl;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LocationModel _location;
    private readonly IParametersManager _parametersManager;
    private readonly PlaceModel _place;
    private readonly LocationModel _startLocation;
    private readonly string _status;
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    //გზის მარშრუტი ერთხელ იძებნება და მენიუს ყოველი გადაწყობისას ხელახლა აღარ ითხოვება
    private (double DistanceKm, TimeSpan Duration)? _roadRoute;
    private bool _roadRouteCounted;

    //მრავალლოკაციიანი ადგილი სიაში თითო ლოკაციაზე თითოჯერ გამოდის და ეს პუნქტი მხოლოდ ერთ,
    //გადმოცემულ ლოკაციას წარმოადგენს — სტატუსში, დეტალებში, მარშრუტსა და მანძილებში ის გამოიყენება.
    //startLocation არჩეული საწყისი წერტილის (FromPoint) ლოკაციაა (Locations ცხრილის ჩანაწერი) — მარშრუტები
    //RouteDistances-ში მისი და ამ ლოკაციის იდენტიფიკატორებით ინახება
    public RecommendedPlaceSubMenuCommand(ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory,
        IHttpClientFactory httpClientFactory, IParametersManager parametersManager, LocationModel startLocation,
        PlaceModel place, LocationModel location, int visitsCount) : base(GetCaptionName(place, location),
        EMenuAction.LoadSubMenu)
    {
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _startLocation = startLocation;
        _place = place;
        _location = location;
        //სტატუსის თავში ამ ლოკაციაზე უკვე დაფიქსირებული ვიზიტების რაოდენობა გამოდის, შემდეგ მისამართი (თუ აქვს);
        //კოორდინატები მძიმით არის გამოყოფილი, რომ Google Maps-ის ძებნაში პირდაპირ ჩაკოპირება შეიძლებოდეს
        string urlPart = place.Url is null ? string.Empty : $"{place.Url} | ";
        _status = string.Create(CultureInfo.InvariantCulture,
            $"{visitsCount} | {urlPart}{location.Latitude}, {location.Longitude}");
        //Google Maps-ის მარშრუტის ბმული: საწყისი წერტილი არჩეული FromPoint-ის ლოკაციაა, საბოლოო — ეს ლოკაცია
        _directionsUrl = string.Create(CultureInfo.InvariantCulture,
            $"https://www.google.com/maps/dir/?api=1&origin={startLocation.Latitude},{startLocation.Longitude}&destination={location.Latitude},{location.Longitude}");
    }

    //ერთი ადგილის რამდენიმე პუნქტს განსხვავებული სახელი უნდა ჰქონდეს — CliMenuSet.GetMenuItemWithName
    //SingleOrDefault-ს იყენებს და გამეორებული სახელი ბოლო ბრძანების გამეორებისას გამონაკლისს ისვრის —
    //ამიტომ მრავალლოკაციიანი ადგილის სახელს ლოკაციის რიგითი ნომერი ემატება
    private static string GetCaptionName(PlaceModel place, LocationModel location)
    {
        string name = place.GetCaption();
        if (place.Locations.Count <= 1)
        {
            return name;
        }

        List<int> locationIds = [.. place.Locations.Select(s => s.LocationId).Order()];
        return string.Create(CultureInfo.InvariantCulture, $"{name}_{locationIds.IndexOf(location.LocationId) + 1}");
    }

    //ბმული და კოორდინატები მენიუში ფრჩხილებში გამოდის და პუნქტის სახელის ნაწილი არ არის,
    //რომ ქვემენიუზე გადასვლისას მენიუს გზაში მხოლოდ სათაური გამოჩნდეს
    protected override string GetStatus()
    {
        return _status;
    }

    public override CliMenuSet GetSubMenu()
    {
        //არჩეული ადგილის ქვემენიუს აგება
        var placeSubMenuSet = new CliMenuSet($"Place => {Name}");

        //არარედაქტირებადი საინფორმაციო პუნქტები. მათი არჩევა არაფერს აკეთებს (EMenuAction.Nothing).
        //არასავალდებულო მონაცემების პუნქტები მხოლოდ მაშინ ემატება, როცა მნიშვნელობა ნამდვილად არსებობს
        placeSubMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Title", _place.GetCaption()));
        if (_place.Url is not null)
        {
            placeSubMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Url", _place.Url));
        }

        //მხოლოდ ის ლოკაცია, რომელსაც ეს პუნქტი წარმოადგენს — ადგილის სხვა ლოკაციები
        //Recommended Visits სიაში ცალკე პუნქტებად გამოდის
        placeSubMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Location",
            string.Create(CultureInfo.InvariantCulture, $"{_location.Latitude}, {_location.Longitude}")));

        //რეგიონი და მუნიციპალიტეტი
        if (_place.RegionNavigation is not null)
        {
            placeSubMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Region",
                _place.RegionNavigation.Name));
        }

        if (_place.MunicipalityNavigation is not null)
        {
            placeSubMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Municipality",
                _place.MunicipalityNavigation.Name));
        }

        //აღწერის სტატუსში ტექსტი ერთ სტრიქონად ჩანს, ნომრის აკრეფისას კი სრული ტექსტის ქვემენიუ იხსნება
        if (!string.IsNullOrWhiteSpace(_place.Description))
        {
            placeSubMenuSet.AddMenuItem(new DescriptionSubMenuCommand(_place.Description));
        }

        //ტეგების სახელები
        if (_place.Tags.Count > 0)
        {
            placeSubMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Tags",
                string.Join(", ",
                    _place.Tags.Select(s => s.TagNavigation.Name).OrderBy(o => o, StringComparer.Ordinal))));
        }

        //რეკომენდებული თვეების ჩამონათვალი კალენდარული თანმიმდევრობით
        if (_place.BestSeasons.Count > 0)
        {
            placeSubMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Best Months",
                string.Join(", ", _place.BestSeasons.OrderBy(o => o.MonthId).Select(s => s.MonthNavigation.Name))));
        }

        //საიტზე მითითებული მანძილები საწყისი წერტილებიდან, ზრდადობით
        if (_place.Distances.Count > 0)
        {
            placeSubMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Distances",
                string.Join(", ",
                    _place.Distances.OrderBy(o => o.Distance).Select(s =>
                        string.Create(CultureInfo.InvariantCulture, $"{s.Distance}კმ {s.FromPointNavigation.Name}")))));
        }

        //გამოთვლილი საჰაერო მანძილი საწყისი წერტილიდან ამ პუნქტის ლოკაციამდე
        double airDistanceKm = DistanceCounter.CountAirDistanceKm(_startLocation.Latitude, _startLocation.Longitude,
            _location.Latitude, _location.Longitude);
        placeSubMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Air Distance",
            string.Create(CultureInfo.InvariantCulture, $"{airDistanceKm:F1}კმ")));

        //გზის მანძილი და სავარაუდო დრო ავტომობილით — ჯერ ბაზიდან, დაუთვლელისთვის OSRM სერვისით
        if (!_roadRouteCounted)
        {
            _roadRoute = GetOrCountRoadRoute(airDistanceKm);
            _roadRouteCounted = true;
        }

        if (_roadRoute is not null)
        {
            placeSubMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Road Distance",
                string.Create(CultureInfo.InvariantCulture,
                    $"{_roadRoute.Value.DistanceKm:F1}კმ, {DistanceCounter.FormatDurationText(_roadRoute.Value.Duration)}")));
        }

        placeSubMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Directions", _directionsUrl));

        //ამ ლოკაციაზე ახალი ვიზიტის დაფიქსირება — ვიზიტი ლოკაციას ებმება და არა ადგილს, რომ მრავალლოკაციიანი
        //ადგილის ერთი ლოკაციის მონახულებამ დანარჩენები ნამყოფად არ აქციოს
        placeSubMenuSet.AddMenuItem(new NewVisitCommand(_travelGuideRepositoryCreatorFactory, _location.LocationId));

        try
        {
            //ამ ლოკაციაზე უკვე დაფიქსირებული ვიზიტების ჩამონათვალი — ვიზიტის არჩევა რედაქტირების ქვემენიუს ხსნის
            ITravelGuideRepository repository = _travelGuideRepositoryCreatorFactory.GetTravelGuideRepository();
            var visitCruder = new VisitCruder(repository, _location.LocationId, _parametersManager);
            foreach (KeyValuePair<string, VisitModel> keyedVisit in visitCruder.GetKeyedVisits())
            {
                placeSubMenuSet.AddMenuItem(new VisitSubMenuCommand(visitCruder, keyedVisit.Value.VisitId,
                    keyedVisit.Key));
            }
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true);
        }

        placeSubMenuSet.AddEscapeCommand("Exit to Recommended Visits menu");
        return placeSubMenuSet;
    }

    //გზის მარშრუტი ჯერ RouteDistances ცხრილში იძებნება და OSRM-ს მხოლოდ დაუთვლელი წყვილისთვის მიემართება;
    //მიღებული პასუხი იმავე ცხრილში ინახება, რომ ამ წყვილზე API-ს მიმართვა მომავალშიც აღარ დასჭირდეს.
    //GetSubMenu გამონაკლისების დამუშავების გარეთ ეშვება, ამიტომ შეცდომისას აქედან გამონაკლისი არ გადის —
    //null ბრუნდება და მენიუ გზის მონაცემების გარეშე აეწყობა
    private (double DistanceKm, TimeSpan Duration)? GetOrCountRoadRoute(double airDistanceKm)
    {
        try
        {
            ITravelGuideRepository repository = _travelGuideRepositoryCreatorFactory.GetTravelGuideRepository();
            RouteDistanceModel? existingRouteDistance =
                repository.GetRouteDistance(_startLocation.LocationId, _location.LocationId);
            if (existingRouteDistance is not null)
            {
                return (existingRouteDistance.RoadDistance, existingRouteDistance.RoadTime);
            }

            Console.WriteLine("Requesting road route from OSRM...");
            (double DistanceKm, TimeSpan Duration)? roadRoute = DistanceCounter.TryGetRoadRoute(_httpClientFactory,
                _startLocation.Latitude, _startLocation.Longitude, _location.Latitude, _location.Longitude);
            if (roadRoute is null)
            {
                return null;
            }

            //Calculate Distances-ის ჩანაწერის იდენტური სტრუქტურა — საჰაერო მანძილიც წყვილთან ერთად ინახება
            repository.AddRouteDistance(new RouteDistanceModel
            {
                StartLocationId = _startLocation.LocationId,
                EndLocationId = _location.LocationId,
                AirDistance = airDistanceKm,
                RoadDistance = roadRoute.Value.DistanceKm,
                RoadTime = roadRoute.Value.Duration
            });
            repository.SaveChanges();
            return roadRoute;
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true);
            return null;
        }
    }
}
