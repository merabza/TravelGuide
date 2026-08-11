using System;
using System.Collections.Generic;
using System.Net.Http;
using AppCliTools.CliMenu;
using DoTravelGuide.Models;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Visits;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class RecommendedVisitsCommand : CliMenuCommand
{
    //რეკომენდებული ადგილების მაქსიმალური რაოდენობა
    private const int RecommendedPlacesCount = 10;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IParametersManager _parametersManager;
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public RecommendedVisitsCommand(IParametersManager parametersManager,
        ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory,
        IHttpClientFactory httpClientFactory) : base("Recommended Visits", EMenuAction.LoadSubMenu)
    {
        _parametersManager = parametersManager;
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
        _httpClientFactory = httpClientFactory;
    }

    public override CliMenuSet GetSubMenu()
    {
        //რეკომენდებული ვიზიტების ქვემენიუს აგება. ეს მეთოდი Run-ის გამონაკლისების დამუშავების გარეთ ეშვება,
        //ამიტომ აქედან გამონაკლისი არ უნდა გავარდეს — შეცდომისას მენიუ ადგილების გარეშე აეწყობა
        var recommendedVisitsMenuSet = new CliMenuSet("Recommended Visits");

        try
        {
            var parameters = (TravelGuideParameters)_parametersManager.Parameters;

            //პარამეტრებში არჩეული უნდა იყოს მიმდინარე ადგილმდებარეობის სახელი
            string? myCurrentPlaceName = parameters.MyCurrentPlaceName;
            if (string.IsNullOrEmpty(myCurrentPlaceName))
            {
                StShared.WriteErrorLine("My Current Place Name is not set in parameters", true);
            }
            //ამ სახელით ჩემს ადგილმდებარეობებში უნდა მოიძებნოს შესაბამისი კოორდინატები
            else if (!parameters.MyPlaces.TryGetValue(myCurrentPlaceName, out MyPlace? myPlace))
            {
                StShared.WriteErrorLine($"My Place with name {myCurrentPlaceName} not found in My Places", true);
            }
            else
            {
                ITravelGuideRepository repository = _travelGuideRepositoryCreatorFactory.GetTravelGuideRepository();

                //ბაზიდან ამოირჩევა ჩემს კოორდინატებთან ყველაზე ახლოს მდებარე ადგილები
                List<PlaceModel> nearestPlaces =
                    repository.GetNearestPlaces(myPlace.Latitude, myPlace.Longitude, RecommendedPlacesCount);

                //თითო ადგილი თითო მენიუს პუნქტად: სახელად მხოლოდ დასახელება, ხოლო ბმული და კოორდინატები
                //პუნქტის სტატუსში, ფრჩხილებში გამოდის
                foreach (PlaceModel place in nearestPlaces)
                {
                    recommendedVisitsMenuSet.AddMenuItem(new RecommendedPlaceSubMenuCommand(
                        _travelGuideRepositoryCreatorFactory, _httpClientFactory, myPlace, place));
                }
            }
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true);
        }

        recommendedVisitsMenuSet.AddEscapeCommand("Exit to Visits menu");
        return recommendedVisitsMenuSet;
    }
}
