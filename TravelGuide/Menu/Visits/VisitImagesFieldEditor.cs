using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.FieldEditors;
using DoTravelGuide.Models;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Visits;

//ვიზიტის სურათების ველი ქვემენიუთი იმართება — ჩამატება და წაშლა VisitImageCruder-ს აქვს
public sealed class VisitImagesFieldEditor : FieldEditor<ICollection<VisitImage>>
{
    private readonly IParametersManager _parametersManager;
    private readonly ITravelGuideRepository _travelGuideRepository;

    // ReSharper disable once ConvertToPrimaryConstructor
    public VisitImagesFieldEditor(string propertyName, ITravelGuideRepository travelGuideRepository,
        IParametersManager parametersManager) : base(propertyName, false, null, false, null, true)
    {
        _travelGuideRepository = travelGuideRepository;
        _parametersManager = parametersManager;
    }

    //ეს მეთოდი მენიუს აგებისას, გამონაკლისების დამუშავების გარეთ ეშვება — გამონაკლისი არ უნდა
    //გავარდეს და null არ უნდა დაბრუნდეს
    public override CliMenuSet GetSubMenu(object record)
    {
        if (record is not VisitModel visit)
        {
            return new CliMenuSet("Visit Images");
        }

        //ჩანაწერი გაყინული ასლია, ამიტომ სია ბაზიდან იდენტიფიკატორით თავიდან იტვირთება.
        //ფოლდერიც ყოველთვის მიმდინარე პარამეტრებიდან იკითხება
        var parameters = (TravelGuideParameters)_parametersManager.Parameters;
        return new VisitImageCruder(_travelGuideRepository, visit.VisitId, parameters.ImagesFolderPath).GetListMenu();
    }

    public override string GetValueStatus(object? record)
    {
        ICollection<VisitImage>? val = GetValue(record);

        if (val is not { Count: > 0 })
        {
            return "No Details";
        }

        return val.Count > 1 ? $"{val.Count} Details" : val.Single().FileName;
    }

    public override void SetDefault(ItemData currentItem)
    {
        //განზრახ ცარიელია: საბაზო რეალიზაცია ახალი ჩანაწერის შექმნისას კოლექციას null-ად აქცევდა
    }
}
