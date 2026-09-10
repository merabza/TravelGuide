using System;
using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using SystemTools.SystemToolsShared;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Lists;

public sealed class ListsSubMenuCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public ListsSubMenuCommand(ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory,
        IHttpClientFactory httpClientFactory) : base("Lists", EMenuAction.LoadSubMenu)
    {
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
        _httpClientFactory = httpClientFactory;
    }

    public override CliMenuSet GetSubMenu()
    {
        //სიების რედაქტორების ქვემენიუს აგება
        var listsSubMenuSet = new CliMenuSet("Lists");

        try
        {
            ITravelGuideRepository repository = _travelGuideRepositoryCreatorFactory.GetTravelGuideRepository();
            //მოტოციკლების სიის რედაქტორი
            listsSubMenuSet.AddMenuItem(new CruderListCliMenuCommand(new MotorcycleCruder(repository)));
            //ადგილების (Places ცხრილის) რედაქტორი — ფილტრით და პორციებად ჩატვირთული სია
            listsSubMenuSet.AddMenuItem(new PlacesCommand(_travelGuideRepositoryCreatorFactory));
            //რეგიონებისა და მუნიციპალიტეტების ცნობარების რედაქტორები
            listsSubMenuSet.AddMenuItem(new CruderListCliMenuCommand(new RegionCruder(repository)));
            listsSubMenuSet.AddMenuItem(new CruderListCliMenuCommand(new MunicipalityCruder(repository)));
            //კატეგორიების, ტეგებისა და მანძილების საწყისი წერტილების ცნობარების რედაქტორები
            listsSubMenuSet.AddMenuItem(new CruderListCliMenuCommand(new CategoryCruder(repository)));
            listsSubMenuSet.AddMenuItem(new CruderListCliMenuCommand(new TagCruder(repository)));
            //საწყისი წერტილის ჩანაწერის მენიუდან მარშრუტები ითვლება (OSRM), ამიტომ მას HttpClient-ის ქარხანა სჭირდება
            listsSubMenuSet.AddMenuItem(
                new CruderListCliMenuCommand(new FromPointCruder(repository, _httpClientFactory)));
        }
        catch (Exception e)
        {
            //ბაზასთან დაკავშირება ვერ მოხერხდა — მენიუ რედაქტორების გარეშე აეწყობა
            StShared.WriteException(e, true);
        }

        listsSubMenuSet.AddEscapeCommand("Exit to Main menu");
        return listsSubMenuSet;
    }
}
