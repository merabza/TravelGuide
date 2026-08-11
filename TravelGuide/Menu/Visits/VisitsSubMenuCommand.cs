using System.Net.Http;
using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Visits;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class VisitsSubMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IParametersManager _parametersManager;
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public VisitsSubMenuCommand(IParametersManager parametersManager,
        ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory,
        IHttpClientFactory httpClientFactory) : base("Visits", EMenuAction.LoadSubMenu)
    {
        _parametersManager = parametersManager;
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
        _httpClientFactory = httpClientFactory;
    }

    public override CliMenuSet GetSubMenu()
    {
        //ვიზიტების ქვემენიუს აგება. ახალი ვიზიტის დაფიქსირება რეკომენდებული ვიზიტების
        //ქვემენიუში, კონკრეტული ადგილის არჩევის შემდეგ ხდება
        var visitsSubMenuSet = new CliMenuSet("Visits");

        //ბოლო ვიზიტების სია
        visitsSubMenuSet.AddMenuItem(new LastVisitsCommand(_travelGuideRepositoryCreatorFactory));
        //რეკომენდებული ვიზიტების სია
        visitsSubMenuSet.AddMenuItem(new RecommendedVisitsCommand(_parametersManager,
            _travelGuideRepositoryCreatorFactory, _httpClientFactory));

        visitsSubMenuSet.AddEscapeCommand("Exit to Main menu");
        return visitsSubMenuSet;
    }
}
