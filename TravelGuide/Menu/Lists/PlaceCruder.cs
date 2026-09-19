using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.CliParameters.Cruders;
using AppCliTools.CliParameters.FieldEditors;
using SystemTools.SystemToolsShared;
using TravelGuide.FieldEditors;
using TravelGuideCore.Domain.PlaceModels;
using TravelGuideCore.Domain.UrlModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Lists;

//ადგილების (Places ცხრილის) რედაქტორი. ჩანაწერის გასაღები წარწერაა (დასახელება, უსახელოსთვის მისამართი,
//არც-მისამართიანისთვის იდენტიფიკატორი — PlaceModelExtensions.GetCaption), რომელიც პორციის ფარგლებში
//უნიკალურია (VisitCruder-ის ყაიდაზე). მისამართი (Urls ცხრილის ჩანაწერი, UrlNavigation) არასავალდებულოა: საიტიდან
//ჩამოტვირთულ ადგილს აქვს, ხელით შეყვანილს კი შექმნისას არ ეკითხება — ის ჩანაწერის მენიუდან, Url ველით ერთხელ
//იწერება (PlaceUrlFieldEditor) და მერე აღარ იცვლება — ქროულერი მისამართს სწორედ ხეშ-კოდით ცნობს; უმისამართო
//ადგილს ქროულერი არ ეხება.
//fieldKeyFromItem=true — ცალკე Record Name ველი არ სჭირდება; მენიუში პუნქტის სახელად წარწერა გამოდის.
//ცხრილი ათასობით ჩანაწერს შეიცავს, ამიტომ ლექსიკონი მთელ ცხრილს კი არა, სიის ბოლოს ჩატვირთულ პორციას
//იჭერს (LoadPortion) — ჩანაწერის მენიუ (ველების რედაქტორები, თანმიმდევრობით რედაქტირება, წაშლა) მასზე
//მუშაობს და დასახელების შეცვლის შემდეგაც იმავე ასლს ხედავს, სანამ სია თავიდან არ ჩაიტვირთება.
//ჩანაწერის მენიუში ველების რედაქტორების შემდეგ Find Location by Name პუნქტია — ლოკაციის დასახელებით ძებნა
//(Nominatim) და ადგილზე მიბმა, ამიტომ რედაქტორს HttpClient-ის ქარხანა სჭირდება
public sealed class PlaceCruder : Cruder
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITravelGuideRepository _travelGuideRepository;

    //ბოლოს ჩატვირთული პორცია წარწერა-გასაღებებით
    private Dictionary<string, ItemData> _portion = new(StringComparer.Ordinal);

    public PlaceCruder(ITravelGuideRepository travelGuideRepository, IHttpClientFactory httpClientFactory) : base(
        "Place", "Places", true)
    {
        _travelGuideRepository = travelGuideRepository;
        _httpClientFactory = httpClientFactory;
        //მისამართი ახალ ადგილს არ ეკითხება (არც შექმნისას, არც თანმიმდევრობით რედაქტირებისას) — ჩანაწერის მენიუდან,
        //მხოლოდ უმისამართო ადგილს მიეწერება (CheckFieldsEnables); ველების სიაში პირველია, რომ მისამართიანი ადგილის
        //უცვლელი Url პუნქტის ადგილას გამოჩნდეს
        FieldEditors.Add(new PlaceUrlFieldEditor(nameof(UrlModel.Url), travelGuideRepository));
        FieldEditors.Add(new OptionalTextFieldEditor(nameof(PlaceModel.Name), true));
        FieldEditors.Add(new DescriptionFieldEditor(nameof(PlaceModel.Description), true));
        //სტატუსი მისამართისაა (UrlModel.State) — რედაქტორი ჩანაწერის UrlNavigation-ზე მუშაობს და უმისამართო
        //ადგილს არ ეკითხება
        FieldEditors.Add(new PlaceStateFieldEditor(nameof(UrlModel.State), true));
        //რეგიონი და მუნიციპალიტეტი შექმნისას არ იკითხება — შენახვისთანავე დასახელებით ძებნა ავსებს (AddRecordWithKey);
        //ჩანაწერის მენიუში ველების რედაქტორები რჩება
        FieldEditors.Add(new LookupIdFieldEditor(nameof(PlaceModel.RegionId), "Region",
            () => travelGuideRepository.GetRegionsList().ToDictionary(k => k.RegionId, v => v.Name)));
        FieldEditors.Add(new LookupIdFieldEditor(nameof(PlaceModel.MunicipalityId), "Municipality",
            () => travelGuideRepository.GetMunicipalitiesList().ToDictionary(k => k.MunicipalityId, v => v.Name)));
        //ლოკაციები ქვერედაქტორით იმართება — შექმნისა და თანმიმდევრობით რედაქტირებისას არ იკითხება
        FieldEditors.Add(new PlaceLocationsFieldEditor(nameof(PlaceModel.Locations), travelGuideRepository));
    }

    //სიის მიმდინარე პორციის ჩატვირთვა ფილტრით (დასახელების ან მისამართის ნაწილი). თითო ჩანაწერს წარწერა
    //(მენიუს პუნქტის სახელი და ჩანაწერის გასაღები) ერთვის; გამეორებული წარწერა რიგითი ნომრით
    //განსხვავდება — CliMenuSet.GetMenuItemWithName SingleOrDefault-ს იყენებს და გამეორებული სახელი
    //ბოლო ბრძანების გამეორებისას გამონაკლისს ისვრის
    public List<KeyValuePair<string, PlaceModel>> LoadPortion(string? filter, int skip, int take)
    {
        List<PlaceModel> places = _travelGuideRepository.GetPlacesPortion(filter, skip, take);

        var captionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        List<KeyValuePair<string, PlaceModel>> keyedPlaces = [];
        foreach (PlaceModel place in places)
        {
            string caption = place.GetCaption();
            int seenCount = captionCounts.GetValueOrDefault(caption);
            captionCounts[caption] = seenCount + 1;
            if (seenCount > 0)
            {
                caption = string.Create(CultureInfo.InvariantCulture, $"{caption} ({seenCount + 1})");
            }

            keyedPlaces.Add(new KeyValuePair<string, PlaceModel>(caption, place));
        }

        _portion = keyedPlaces.ToDictionary(k => k.Key, ItemData (v) => v.Value, StringComparer.Ordinal);
        return keyedPlaces;
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        return _portion;
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        return _portion.ContainsKey(recordKey);
    }

    //ახალი ადგილი უმისამართოდ იქმნება — მისამართი არ იკითხება, ის ჩანაწერის მენიუდან, Url ველით მიეწერება
    //(PlaceUrlFieldEditor); საბაზისო ItemData-ს მაგივრად PlaceModel უნდა დაბრუნდეს
    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        return new PlaceModel();
    }

    //Url ველის რედაქტორი მხოლოდ უმისამართო ადგილს აქვს — მიწერილი მისამართი აღარ იცვლება და ჩანაწერის მენიუში
    //უცვლელ პუნქტად ჩანს (FillDetailsSubMenu)
    protected override void CheckFieldsEnables(ItemData itemData, string? lastEditedFieldName = null)
    {
        EnableFieldByName(nameof(UrlModel.Url), itemData is PlaceModel { UrlNavigation: null });
    }

    //ჩანაწერის მენიუში ველების რედაქტორების წინ მისამართი უცვლელი, საინფორმაციო პუნქტად ჩანს — მხოლოდ მაშინ,
    //როცა ადგილს მისამართი აქვს; უმისამართო ადგილს იმავე ადგილას Url ველის რედაქტორი უჩანს (CheckFieldsEnables)
    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string itemName)
    {
        if (_portion.TryGetValue(itemName, out ItemData? itemData) &&
            itemData is PlaceModel { UrlNavigation.Url: { } url })
        {
            itemSubMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Url", url));
        }

        base.FillDetailsSubMenu(itemSubMenuSet, itemName);

        //ველების რედაქტორების შემდეგ ლოკაციის დასახელებით ძებნის პუნქტი. ბრძანებას ადგილის ის ასლი გადაეცემა,
        //რომელსაც ველების რედაქტორები ცვლიან — დასახელება მასში მიმდინარეა, ხოლო რეგიონისა და მუნიციპალიტეტის
        //განახლებას ბრძანება მასზე წერს და ამ რედაქტორით (UpdateRecordWithKey) ინახავს
        if (itemData is PlaceModel place)
        {
            itemSubMenuSet.AddMenuItem(new FindLocationByNameCommand(this, _travelGuideRepository, _httpClientFactory,
                place, itemName));
        }
    }

    protected override async ValueTask AddRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        //recordKey აქ შემთხვევითი Guid-ია (fieldKeyFromItem) — ჩანაწერს გასაღები (წარწერა) სიის ჩატვირთვისას
        //ენიჭება; ახალ ადგილს მისამართი არ აქვს (CreateNewItem)
        if (newRecord is not PlaceModel newPlace)
        {
            return;
        }

        _travelGuideRepository.AddPlace(newPlace);

        _travelGuideRepository.SaveChanges();

        //შენახვისთანავე (იდენტიფიკატორი უკვე აქვს) რეგიონი, მუნიციპალიტეტი და ლოკაცია დასახელებით ისაზღვრება — იგივე
        //ბრძანებით, რაც ჩანაწერის მენიუშია, ოღონდ საძიებო ტექსტის კითხვის გარეშე; Escape-სა და შეცდომებს ბრძანების Run
        //თავად იჭერს — ადგილი უკვე შენახულია და ლოკაციის გარეშე რჩება. უსახელო ადგილს საძიებო არაფერი აქვს
        if (!string.IsNullOrWhiteSpace(newPlace.Name))
        {
            await new FindLocationByNameCommand(this, _travelGuideRepository, _httpClientFactory, newPlace,
                newPlace.GetCaption(), false).Run(cancellationToken);
        }
    }

    public override ValueTask UpdateRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        if (newRecord is not PlaceModel newPlace)
        {
            return ValueTask.CompletedTask;
        }

        //რედაქტორები მოუბმელ ასლს ცვლიან; შესანახად ბმული ჩანაწერი იდენტიფიკატორით მოიძებნება
        PlaceModel place = _travelGuideRepository.GetPlaceById(newPlace.PlaceId) ??
                           throw new InvalidOperationException($"Place with id {newPlace.PlaceId} not found");
        place.Name = newPlace.Name;
        place.Description = newPlace.Description;
        place.RegionId = newPlace.RegionId;
        place.MunicipalityId = newPlace.MunicipalityId;
        if (place.UrlNavigation is null && newPlace.UrlNavigation is not null)
        {
            //უმისამართო ადგილს მისამართი Url ველის რედაქტორმა ასლზე მიაწერა (PlaceUrlFieldEditor-მა უკვე შეამოწმა) —
            //ბმულ ჩანაწერს ახალი Urls-ის ჩანაწერი ებმება და ადგილთან ერთად ინახება
            place.UrlNavigation = new UrlModel
            {
                Url = newPlace.UrlNavigation.Url,
                UrlHashCode = newPlace.UrlNavigation.UrlHashCode,
                State = newPlace.UrlNavigation.State
            };
        }
        else if (place.UrlNavigation is not null && newPlace.UrlNavigation is not null)
        {
            //სტატუსი მისამართისაა და მხოლოდ მისამართიან ადგილს აქვს — ბმული მისამართი GetPlaceById-ს აქვს ჩატვირთული
            place.UrlNavigation.State = newPlace.UrlNavigation.State;
        }

        //ლოკაციები ცალკე ქვერედაქტორით (PlaceLocationCruder) იმართება და აქ არ კოპირდება
        _travelGuideRepository.UpdatePlace(place);

        _travelGuideRepository.SaveChanges();
        return ValueTask.CompletedTask;
    }

    protected override ValueTask RemoveRecordWithKey(string recordKey, CancellationToken cancellationToken = default)
    {
        if (!_portion.TryGetValue(recordKey, out ItemData? itemData) || itemData is not PlaceModel placeCopy)
        {
            throw new InvalidOperationException($"Place with key {recordKey} not found");
        }

        //ვიზიტებიანი ადგილი არ იშლება — ვიზიტები ადგილის ლოკაციებზეა და ადგილის წაშლისას (ბმულების კასკადით
        //წაშლის შემდეგ) ისინი უსახელო ლოკაციაზე დარჩებოდა; ჯერ ვიზიტები უნდა წაიშალოს
        List<int> locationIds =
            [.. _travelGuideRepository.GetPlaceLocations(placeCopy.PlaceId).Select(s => s.LocationId)];
        if (locationIds.Count > 0 && _travelGuideRepository.GetVisitCountsByLocationIds(locationIds).Count > 0)
        {
            StShared.WriteErrorLine(
                $"Place {recordKey} has Visits on its Locations and cannot be deleted. Delete its Visits first", true);
            return ValueTask.CompletedTask;
        }

        PlaceModel place = _travelGuideRepository.GetPlaceById(placeCopy.PlaceId) ??
                           throw new InvalidOperationException($"Place with id {placeCopy.PlaceId} not found");

        //მისამართის ჩანაწერი (Urls) ადგილთან ერთად იშლება, რომ ხელახალი ქროულინგისას მისამართი ისევ ახლად
        //აღმოჩენილად ჩაითვალოს; ბმულების გრაფის წიბოები Urls-ზე Restrict-ით არის მიბმული და წინასწარ, ცალკე იშლება.
        //დანარჩენი შვილობილი ჩანაწერები (ლოკაციები, ტეგები, კატეგორიები, სეზონები, მანძილები) კასკადით მიჰყვება
        if (place.UrlNavigation is not null)
        {
            _travelGuideRepository.DeleteUrlGraphNodesByUrlId(place.UrlNavigation.UrlId);
            _travelGuideRepository.DeleteUrl(place.UrlNavigation);
        }

        _travelGuideRepository.DeletePlace(place);

        _travelGuideRepository.SaveChanges();
        return ValueTask.CompletedTask;
    }
}
