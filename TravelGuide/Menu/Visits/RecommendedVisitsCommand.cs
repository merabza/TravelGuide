using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using AppCliTools.LibMenuInput;
using DoTravelGuide.Models;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;
using TravelGuide.Menu.TravelGuideParametersEdit;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Visits;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class RecommendedVisitsCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IParametersManager _parametersManager;
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    //მიმდინარე პორციის ნომერი. გადაფურცვლისას იცვლება და მენიუს გადაწყობებს შორის ინახება
    //(მაგალითად ადგილიდან უკან დაბრუნებისას იგივე პორცია რჩება); ქვემენიუში თავიდან შესვლისას
    //ბრძანება ახლიდან იქმნება და სია ისევ პირველი პორციიდან იწყება
    private int _currentPortionNumber;

    //მაქსიმალური ვიზიტების რაოდენობაც ქვემენიუში შესვლისას ერთხელ შეჰყავს მომხმარებელს და მენიუს
    //გადაწყობებზე ხელახლა აღარ იკითხება
    private int _maxVisitsCount;

    //მინიმალური გზის დრო ქვემენიუში შესვლისას ერთხელ ირჩევა და მენიუს ყოველ გადაწყობაზე
    //(მაგალითად ადგილიდან უკან დაბრუნებისას) ხელახლა აღარ იკითხება
    private TimeSpan _minRoadTime;

    public RecommendedVisitsCommand(IParametersManager parametersManager,
        ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory,
        IHttpClientFactory httpClientFactory) : base("Recommended Visits", EMenuAction.LoadSubMenu)
    {
        _parametersManager = parametersManager;
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
        _httpClientFactory = httpClientFactory;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        var parameters = (TravelGuideParameters)_parametersManager.Parameters;

        //პარამეტრებში ჩაწერილი მინიმალური დროებიდან ერთ-ერთს მომხმარებელი ირჩევს; ცარიელი სიისას
        //არჩევანი საჭირო არ არის — 0:00 ფილტრს თიშავს. გამეორებული დროები ერთხელ გამოდის და
        //Enter უმცირესს ირჩევს. Escape-ის გამონაკლისს საბაზო Run იჭერს და ქვემენიუ არ იხსნება
        List<TimeSpan> minDistanceTimes =
            [.. parameters.MinDistanceTimes.DistinctBy(MinDistanceTimeCruder.GetKey).Order()];
        if (minDistanceTimes.Count == 0)
        {
            _minRoadTime = TimeSpan.Zero;
        }
        else
        {
            List<string> keys = [.. minDistanceTimes.Select(MinDistanceTimeCruder.GetKey)];
            var selectFromListInput = new SelectFromListInput("Minimal Road Time", keys, keys[0]);
            if (!selectFromListInput.DoInput() || selectFromListInput.Text is null)
            {
                return ValueTask.FromResult(false);
            }

            _minRoadTime = minDistanceTimes[keys.IndexOf(selectFromListInput.Text)];
        }

        //მაქსიმალური ვიზიტების რაოდენობა — სიაში დარჩება მხოლოდ ის ადგილები, რომლებზეც ვიზიტები ამდენჯერ
        //ან ნაკლებჯერაა დაფიქსირებული. შემყვანი მხოლოდ ციფრებს იღებს, ამიტომ უარყოფითი რიცხვი ვერ
        //შეიყვანება; ცარიელ შეყვანაზე Enter ნაგულისხმევ 0-ს ირჩევს — მხოლოდ მოუნახულებელი ადგილები რჩება
        var maxVisitsCountInput = new IntDataInput("Maximal Visits Count");
        if (!maxVisitsCountInput.DoInput())
        {
            return ValueTask.FromResult(false);
        }

        _maxVisitsCount = maxVisitsCountInput.Value;
        return ValueTask.FromResult(true);
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

                //პორციის ზომა მიმდინარე კონსოლის ფანჯრის სიმაღლიდან ითვლება CliMenuSet.Show-ს ფორმულის
                //მიხედვით: 7 სტრიქონი სათაურს, ცარიელ სტრიქონებსა და არჩევანის მოთხოვნას მიაქვს, კიდევ 3 —
                //Escape-სა და გადაფურცვლის ორ ღილაკს. პორცია ამ ზომას რომ აჭარბებდეს, CliMenuSet საკუთარ
                //გადაფურცვლას ჩართავდა და მისი PageUp/PageDown გასაღებები ჩვენსას დაეჯახებოდა.
                //62-ზე მეტ პუნქტს მენიუს გასაღებები (0-9, a-z, A-Z) აღარ ჰყოფნის
                int portionSize = Math.Clamp(Console.WindowHeight - 10, 1, 62);

                //ბაზიდან ამოირჩევა ჩემს კოორდინატებთან ყველაზე ახლოს მდებარე ადგილი-ლოკაციის ბმულების
                //მიმდინარე პორცია (რომლებამდე გზის დროც არჩეულ მინიმუმზე ნაკლები არ არის და რომლების
                //ვიზიტების რაოდენობაც შეყვანილ მაქსიმუმს არ აღემატება). ერთით მეტი ჩანაწერი ითხოვება,
                //რომ გაირკვეს, არსებობს თუ არა შემდეგი პორცია
                List<PlaceByLocation> nearestPlaceLocations = repository.GetNearestPlaces(myPlace.Latitude,
                    myPlace.Longitude, _currentPortionNumber * portionSize, portionSize + 1, _minRoadTime,
                    _maxVisitsCount);

                //პორციის ადგილებზე დაფიქსირებული ვიზიტების რაოდენობები ერთი მოთხოვნით იტვირთება —
                //თითო პუნქტის სტატუსის თავში გამოსატანად
                List<int> placeIds = [.. nearestPlaceLocations.Take(portionSize).Select(s => s.PlaceId).Distinct()];
                Dictionary<int, int> visitCounts = repository.GetVisitCountsByPlaceIds(placeIds);

                //თითო ადგილი-ლოკაციის წყვილი თითო მენიუს პუნქტად — მრავალლოკაციიანი ადგილი იმდენჯერ გამოდის,
                //რამდენი ლოკაციაც აქვს. სახელად დასახელება, ხოლო ვიზიტების რაოდენობა, ბმული და კოორდინატები
                //პუნქტის სტატუსში, ფრჩხილებში გამოდის
                foreach (PlaceByLocation placeByLocation in nearestPlaceLocations.Take(portionSize))
                {
                    recommendedVisitsMenuSet.AddMenuItem(new RecommendedPlaceSubMenuCommand(
                        _travelGuideRepositoryCreatorFactory, _httpClientFactory, myPlace,
                        placeByLocation.PlaceNavigation, placeByLocation.LocationNavigation,
                        visitCounts.GetValueOrDefault(placeByLocation.PlaceId)));
                }

                //გადაფურცვლის ღილაკები ფიზიკურ PageUp/PageDown კლავიშებზეა მიბმული ისევე, როგორც
                //CliMenuSet-ის ჩაშენებულ გადაფურცვლაში, და მხოლოდ მაშინ ჩანს, როცა შესაბამისი პორცია არსებობს
                if (_currentPortionNumber > 0)
                {
                    recommendedVisitsMenuSet.AddMenuItem(ConsoleKey.PageUp.Value().Pascalize(),
                        new ChangePortionCommand("Page Up", () => _currentPortionNumber--));
                }

                if (nearestPlaceLocations.Count > portionSize)
                {
                    recommendedVisitsMenuSet.AddMenuItem(ConsoleKey.PageDown.Value().Pascalize(),
                        new ChangePortionCommand("Page Down", () => _currentPortionNumber++));
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
