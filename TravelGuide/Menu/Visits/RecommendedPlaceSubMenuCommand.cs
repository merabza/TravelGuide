using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using DoTravelGuide.Models;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;
using TravelGuide.Menu.Distances;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Visits;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class RecommendedPlaceSubMenuCommand : CliMenuCommand
{
    private readonly string _directionsUrl;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly LocationModel _location;
    private readonly MyPlace _myPlace;
    private readonly IParametersManager _parametersManager;
    private readonly PlaceModel _place;
    private readonly string _status;
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    //გზის მარშრუტი ერთხელ იძებნება და მენიუს ყოველი გადაწყობისას ხელახლა აღარ ითხოვება
    private (double DistanceKm, TimeSpan Duration)? _roadRoute;
    private bool _roadRouteCounted;

    //მრავალლოკაციიანი ადგილი სიაში თითო ლოკაციაზე თითოჯერ გამოდის და ეს პუნქტი მხოლოდ ერთ,
    //გადმოცემულ ლოკაციას წარმოადგენს — სტატუსში, დეტალებში, მარშრუტსა და მანძილებში ის გამოიყენება
    public RecommendedPlaceSubMenuCommand(ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory,
        IHttpClientFactory httpClientFactory, IParametersManager parametersManager, MyPlace myPlace, PlaceModel place,
        LocationModel location, int visitsCount) : base(GetCaptionName(place, location), EMenuAction.LoadSubMenu)
    {
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        _myPlace = myPlace;
        _place = place;
        _location = location;
        //სტატუსის თავში ამ ადგილზე უკვე დაფიქსირებული ვიზიტების რაოდენობა გამოდის; კოორდინატები მძიმით
        //არის გამოყოფილი, რომ Google Maps-ის ძებნაში პირდაპირ ჩაკოპირება შეიძლებოდეს
        _status = string.Create(CultureInfo.InvariantCulture,
            $"{visitsCount} | {place.Url} | {location.Latitude}, {location.Longitude}");
        //Google Maps-ის მარშრუტის ბმული: საწყისი წერტილი ჩემი მიმდინარე ადგილმდებარეობაა, საბოლოო — ეს ლოკაცია
        _directionsUrl = string.Create(CultureInfo.InvariantCulture,
            $"https://www.google.com/maps/dir/?api=1&origin={myPlace.Latitude},{myPlace.Longitude}&destination={location.Latitude},{location.Longitude}");
    }

    //ერთი ადგილის რამდენიმე პუნქტს განსხვავებული სახელი უნდა ჰქონდეს — CliMenuSet.GetMenuItemWithName
    //SingleOrDefault-ს იყენებს და გამეორებული სახელი ბოლო ბრძანების გამეორებისას გამონაკლისს ისვრის —
    //ამიტომ მრავალლოკაციიანი ადგილის სახელს ლოკაციის რიგითი ნომერი ემატება
    private static string GetCaptionName(PlaceModel place, LocationModel location)
    {
        string name = place.Name ?? place.Url;
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
        placeSubMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Title", _place.Name ?? _place.Url));
        placeSubMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Url", _place.Url));

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

        //გამოთვლილი საჰაერო მანძილი ჩემი მიმდინარე ადგილმდებარეობიდან ამ პუნქტის ლოკაციამდე
        double airDistanceKm = DistanceCounter.CountAirDistanceKm(_myPlace.Latitude, _myPlace.Longitude,
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

        //ამ ადგილზე ახალი ვიზიტის დაფიქსირება
        placeSubMenuSet.AddMenuItem(new NewVisitCommand(_travelGuideRepositoryCreatorFactory, _place.PlaceId));

        try
        {
            //ამ ადგილზე უკვე დაფიქსირებული ვიზიტების ჩამონათვალი — ვიზიტის არჩევა რედაქტირების ქვემენიუს ხსნის
            ITravelGuideRepository repository = _travelGuideRepositoryCreatorFactory.GetTravelGuideRepository();
            var visitCruder = new VisitCruder(repository, _place.PlaceId, _parametersManager);
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
            RouteDistanceModel? existingRouteDistance = repository.GetRouteDistance(_myPlace.Latitude,
                _myPlace.Longitude, _location.Latitude, _location.Longitude);
            if (existingRouteDistance is not null)
            {
                return (existingRouteDistance.RoadDistance, existingRouteDistance.RoadTime);
            }

            Console.WriteLine("Requesting road route from OSRM...");
            (double DistanceKm, TimeSpan Duration)? roadRoute = DistanceCounter.TryGetRoadRoute(_httpClientFactory,
                _myPlace.Latitude, _myPlace.Longitude, _location.Latitude, _location.Longitude);
            if (roadRoute is null)
            {
                return null;
            }

            //Calculate Distances-ის ჩანაწერის იდენტური სტრუქტურა — საჰაერო მანძილიც წყვილთან ერთად ინახება
            repository.AddRouteDistance(new RouteDistanceModel
            {
                StartLatitude = _myPlace.Latitude,
                StartLongitude = _myPlace.Longitude,
                EndLatitude = _location.Latitude,
                EndLongitude = _location.Longitude,
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
