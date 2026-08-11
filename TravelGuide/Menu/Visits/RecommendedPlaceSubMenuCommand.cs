using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using DoTravelGuide.Models;
using TravelGuide.Menu.Distances;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Visits;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class RecommendedPlaceSubMenuCommand : CliMenuCommand
{
    private readonly string? _directionsUrl;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MyPlace _myPlace;
    private readonly LocationModel? _nearestLocation;
    private readonly PlaceModel _place;
    private readonly string _status;
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    //გზის მარშრუტი ერთხელ იძებნება და მენიუს ყოველი გადაწყობისას ხელახლა აღარ ითხოვება
    private (double DistanceKm, TimeSpan Duration)? _roadRoute;
    private bool _roadRouteCounted;

    public RecommendedPlaceSubMenuCommand(ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory,
        IHttpClientFactory httpClientFactory, MyPlace myPlace, PlaceModel place) : base(place.Name ?? place.Url,
        EMenuAction.LoadSubMenu)
    {
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
        _httpClientFactory = httpClientFactory;
        _myPlace = myPlace;
        _place = place;
        //ადგილს რამდენიმე ლოკაცია შეიძლება ჰქონდეს — სტატუსში, მარშრუტსა და მანძილებში ჩემს
        //მიმდინარე ადგილმდებარეობასთან ყველაზე ახლო ლოკაცია გამოიყენება
        _nearestLocation = place.Locations.Select(s => s.LocationNavigation).MinBy(m =>
            DistanceCounter.CountAirDistanceKm(myPlace.Latitude, myPlace.Longitude, m.Latitude, m.Longitude));
        //კოორდინატები მძიმით არის გამოყოფილი, რომ Google Maps-ის ძებნაში პირდაპირ ჩაკოპირება შეიძლებოდეს
        _status = _nearestLocation is null
            ? place.Url
            : string.Create(CultureInfo.InvariantCulture,
                $"{place.Url} | {_nearestLocation.Latitude}, {_nearestLocation.Longitude}");
        //Google Maps-ის მარშრუტის ბმული: საწყისი წერტილი ჩემი მიმდინარე ადგილმდებარეობაა, საბოლოო — ეს ადგილი
        _directionsUrl = _nearestLocation is null
            ? null
            : string.Create(CultureInfo.InvariantCulture,
                $"https://www.google.com/maps/dir/?api=1&origin={myPlace.Latitude},{myPlace.Longitude}&destination={_nearestLocation.Latitude},{_nearestLocation.Longitude}");
    }

    //ბმული და კოორდინატები მენიუში ფრჩხილებში გამოდის და პუნქტის სახელის ნაწილი არ არის,
    //რომ ქვემენიუზე გადასვლისას მენიუს გზაში მხოლოდ სათაური გამოჩნდეს
    protected override string? GetStatus()
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

        //ადგილის ყველა ლოკაცია ცალ-ცალკე პუნქტად; წარწერები უნიკალური უნდა იყოს, რადგან
        //CliMenuSet.GetMenuItemWithName SingleOrDefault-ს იყენებს და გამეორებული სახელი ბოლო
        //ბრძანების გამეორებისას გამონაკლისს ისვრის
        List<LocationModel> locations =
            [.. _place.Locations.Select(s => s.LocationNavigation).OrderBy(o => o.LocationId)];
        for (var index = 0; index < locations.Count; index++)
        {
            string caption = locations.Count == 1
                ? "Location"
                : string.Create(CultureInfo.InvariantCulture, $"Location {index + 1}");
            placeSubMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand(caption,
                string.Create(CultureInfo.InvariantCulture,
                    $"{locations[index].Latitude}, {locations[index].Longitude}")));
        }

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
                        string.Create(CultureInfo.InvariantCulture,
                            $"{s.Distance}კმ {s.FromPointNavigation.Name}")))));
        }

        //გამოთვლილი საჰაერო მანძილი ჩემი მიმდინარე ადგილმდებარეობიდან ამ ადგილის უახლოეს ლოკაციამდე
        if (_nearestLocation is not null)
        {
            double airDistanceKm = DistanceCounter.CountAirDistanceKm(_myPlace.Latitude, _myPlace.Longitude,
                _nearestLocation.Latitude, _nearestLocation.Longitude);
            placeSubMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Air Distance",
                string.Create(CultureInfo.InvariantCulture, $"{airDistanceKm:F1}კმ")));

            //გზის მანძილი და სავარაუდო დრო ავტომობილით, OSRM სერვისით
            if (!_roadRouteCounted)
            {
                Console.WriteLine("Requesting road route from OSRM...");
                _roadRoute = DistanceCounter.TryGetRoadRoute(_httpClientFactory, _myPlace.Latitude,
                    _myPlace.Longitude, _nearestLocation.Latitude, _nearestLocation.Longitude);
                _roadRouteCounted = true;
            }

            if (_roadRoute is not null)
            {
                placeSubMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Road Distance",
                    string.Create(CultureInfo.InvariantCulture,
                        $"{_roadRoute.Value.DistanceKm:F1}კმ, {DistanceCounter.FormatDurationText(_roadRoute.Value.Duration)}")));
            }
        }

        if (_directionsUrl is not null)
        {
            placeSubMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Directions", _directionsUrl));
        }

        //ამ ადგილზე ახალი ვიზიტის დაფიქსირება
        placeSubMenuSet.AddMenuItem(new NewVisitCommand(_travelGuideRepositoryCreatorFactory, _place.PlaceId));

        placeSubMenuSet.AddEscapeCommand("Exit to Recommended Visits menu");
        return placeSubMenuSet;
    }
}
