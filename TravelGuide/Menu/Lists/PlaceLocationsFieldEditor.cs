using System;
using System.Collections.Generic;
using System.Globalization;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.FieldEditors;
using SystemTools.SystemToolsShared;
using TravelGuideCore.Domain.PlaceModels;
using TravelGuideCore.Domain.PlacesByLocations;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Lists;

//ადგილის ლოკაციების ველი ქვემენიუთი იმართება — ჩამატება, შეცვლა და მოხსნა PlaceLocationCruder-ს აქვს.
//ჩანაწერი (ადგილის მოუბმელი ასლი) ბმულებს არ შეიცავს და ქვერედაქტორში შეცვლისას აღარც განახლდებოდა,
//ამიტომ სტატუსიც და სიაც ბაზიდან ადგილის იდენტიფიკატორით იტვირთება
public sealed class PlaceLocationsFieldEditor : FieldEditor
{
    private readonly ITravelGuideRepository _travelGuideRepository;

    public PlaceLocationsFieldEditor(string propertyName, ITravelGuideRepository travelGuideRepository) : base(
        propertyName, null, true)
    {
        _travelGuideRepository = travelGuideRepository;
    }

    //ეს მეთოდი მენიუს აგებისას, გამონაკლისების დამუშავების გარეთ ეშვება — გამონაკლისი არ უნდა
    //გავარდეს და null არ უნდა დაბრუნდეს
    public override CliMenuSet GetSubMenu(object record)
    {
        if (record is not PlaceModel place)
        {
            return new CliMenuSet(FieldName);
        }

        return new PlaceLocationCruder(_travelGuideRepository, place.PlaceId).GetListMenu();
    }

    //სტატუსი მენიუს ხატვისას გამონაკლისების დამუშავების გარეშე ითხოვება, ამიტომ შეცდომისას ცარიელი ბრუნდება
    public override string GetValueStatus(object? record)
    {
        if (record is not PlaceModel place)
        {
            return string.Empty;
        }

        try
        {
            List<PlaceByLocation> links = _travelGuideRepository.GetPlaceLocations(place.PlaceId);
            return links.Count switch
            {
                0 => "No Locations",
                1 => new LocationItem(links[0].LocationNavigation).GetItemKey(),
                _ => string.Create(CultureInfo.InvariantCulture, $"{links.Count} Locations")
            };
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true);
            return string.Empty;
        }
    }
}
