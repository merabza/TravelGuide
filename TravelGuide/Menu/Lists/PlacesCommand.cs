using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.LibDataInput;
using SystemTools.SystemToolsShared;
using TravelGuide.Menu.Visits;
using TravelGuideCore.Domain.PlaceModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Lists;

//ადგილების (Places ცხრილის) რედაქტორის სია. ცხრილი ათასობით ჩანაწერს შეიცავს, ამიტომ სია Recommended
//Visits-ის ყაიდაზე ბაზიდან პორციებად იტვირთება (PageUp/PageDown) და შესვლისას ფილტრი იკითხება
public sealed class PlacesCommand : CliMenuCommand
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    //მიმდინარე პორციის ნომერი — გადაფურცვლისას იცვლება და მენიუს გადაწყობებს შორის ინახება
    //(მაგალითად ჩანაწერიდან უკან დაბრუნებისას იგივე პორცია რჩება)
    private int _currentPortionNumber;

    //ფილტრი ქვემენიუში შესვლისას ერთხელ იკითხება და მენიუს გადაწყობებზე ხელახლა აღარ იკითხება
    private string? _filter;

    //ადგილის ჩანაწერის მენიუდან ლოკაცია დასახელებით იძებნება (Nominatim), ამიტომ რედაქტორს HttpClient-ის ქარხანა სჭირდება
    public PlacesCommand(ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory,
        IHttpClientFactory httpClientFactory) : base("Places", EMenuAction.LoadSubMenu)
    {
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
        _httpClientFactory = httpClientFactory;
    }

    //სტატუსში ცხრილის ჩანაწერების საერთო რაოდენობა ჩანს (როგორც Motorcycles-ს). სტატუსი მენიუს ხატვისას
    //გამონაკლისების დამუშავების გარეშე ითხოვება, ამიტომ შეცდომისას null ბრუნდება
    protected override string? GetStatus()
    {
        try
        {
            return _travelGuideRepositoryCreatorFactory.GetTravelGuideRepository().GetPlacesCount()
                .ToString(CultureInfo.InvariantCulture);
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true);
            return null;
        }
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        //ფილტრი დასახელების ან მისამართის ნაწილია; ცარიელი Enter ფილტრს თიშავს და ყველა ჩანაწერი გამოდის.
        //Escape-ის გამონაკლისს საბაზო Run იჭერს და ქვემენიუ არ იხსნება
        _filter = Inputer.InputText("Name or Url Filter", null);
        _currentPortionNumber = 0;
        return ValueTask.FromResult(true);
    }

    public override CliMenuSet GetSubMenu()
    {
        //ეს მეთოდი Run-ის გამონაკლისების დამუშავების გარეთ ეშვება, ამიტომ აქედან გამონაკლისი არ უნდა
        //გავარდეს — შეცდომისას მენიუ ადგილების გარეშე აეწყობა
        var placesMenuSet = new CliMenuSet("Places");

        try
        {
            ITravelGuideRepository repository = _travelGuideRepositoryCreatorFactory.GetTravelGuideRepository();
            var placeCruder = new PlaceCruder(repository, _httpClientFactory);

            //ახალი ადგილის ხელით შექმნა (მაგალითად საიტზე არარსებული ადგილისთვის)
            placesMenuSet.AddMenuItem(new NewItemCliMenuCommand(placeCruder, placeCruder.CrudNamePlural,
                $"New {placeCruder.CrudName}"));

            //პორციის ზომა კონსოლის სიმაღლიდან ითვლება CliMenuSet.Show-ს ფორმულით (იხ. RecommendedVisitsCommand):
            //7 სტრიქონი სათაურსა და მოთხოვნას მიაქვს, 3 — Escape-სა და გადაფურცვლის ღილაკებს, 1 — New Place-ს;
            //62-ზე მეტ უკლავიშო პუნქტს მენიუს გასაღებები (0-9, a-z, A-Z) აღარ ჰყოფნის
            int portionSize = Math.Clamp(Console.WindowHeight - 11, 1, 61);

            //ერთით მეტი ჩანაწერი ითხოვება, რომ გაირკვეს, არსებობს თუ არა შემდეგი პორცია
            List<KeyValuePair<string, PlaceModel>> keyedPlaces = placeCruder.LoadPortion(_filter,
                _currentPortionNumber * portionSize, portionSize + 1);

            foreach (KeyValuePair<string, PlaceModel> keyedPlace in keyedPlaces.Take(portionSize))
            {
                placesMenuSet.AddMenuItem(new PlaceSubMenuCommand(placeCruder, keyedPlace.Value, keyedPlace.Key));
            }

            //გადაფურცვლის ღილაკები ფიზიკურ PageUp/PageDown კლავიშებზეა მიბმული და მხოლოდ მაშინ ჩანს,
            //როცა შესაბამისი პორცია არსებობს
            if (_currentPortionNumber > 0)
            {
                placesMenuSet.AddMenuItem(ConsoleKey.PageUp.Value().Pascalize(),
                    new ChangePortionCommand("Page Up", () => _currentPortionNumber--));
            }

            if (keyedPlaces.Count > portionSize)
            {
                placesMenuSet.AddMenuItem(ConsoleKey.PageDown.Value().Pascalize(),
                    new ChangePortionCommand("Page Down", () => _currentPortionNumber++));
            }
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true);
        }

        placesMenuSet.AddEscapeCommand("Exit to Lists menu");
        return placesMenuSet;
    }
}
