using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.CliParameters.Cruders;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.LibDataInput;
using SystemTools.SystemToolsShared;
using TravelGuide.FieldEditors;
using TravelGuideCore.Domain;
using TravelGuideCore.Domain.PlaceModels;
using TravelGuideCore.Domain.UrlModels;
using TravelGuideDbPart.Db.Configurations;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Lists;

//ადგილების (Places ცხრილის) რედაქტორი. ჩანაწერის გასაღები წარწერაა (დასახელება, უსახელოსთვის მისამართი,
//არც-მისამართიანისთვის იდენტიფიკატორი — PlaceModelExtensions.GetCaption), რომელიც პორციის ფარგლებში
//უნიკალურია (VisitCruder-ის ყაიდაზე). მისამართი (Urls ცხრილის ჩანაწერი, UrlNavigation) არასავალდებულოა: საიტიდან
//ჩამოტვირთულ ადგილს აქვს და შექმნისას ერთხელ იწერება, მერე აღარ იცვლება — ქროულერი მისამართს სწორედ ხეშ-კოდით
//ცნობს; ხელით შეყვანილ ადგილს მისამართი არ აქვს და ქროულერი მას არ ეხება.
//fieldKeyFromItem=true — ცალკე Record Name ველი არ სჭირდება; მენიუში პუნქტის სახელად წარწერა გამოდის.
//ცხრილი ათასობით ჩანაწერს შეიცავს, ამიტომ ლექსიკონი მთელ ცხრილს კი არა, სიის ბოლოს ჩატვირთულ პორციას
//იჭერს (LoadPortion) — ჩანაწერის მენიუ (ველების რედაქტორები, თანმიმდევრობით რედაქტირება, წაშლა) მასზე
//მუშაობს და დასახელების შეცვლის შემდეგაც იმავე ასლს ხედავს, სანამ სია თავიდან არ ჩაიტვირთება
public sealed class PlaceCruder : Cruder
{
    private readonly ITravelGuideRepository _travelGuideRepository;

    //ბოლოს ჩატვირთული პორცია წარწერა-გასაღებებით
    private Dictionary<string, ItemData> _portion = new(StringComparer.Ordinal);

    public PlaceCruder(ITravelGuideRepository travelGuideRepository) : base("Place", "Places", true)
    {
        _travelGuideRepository = travelGuideRepository;
        FieldEditors.Add(new OptionalTextFieldEditor(nameof(PlaceModel.Name), true));
        FieldEditors.Add(new DescriptionFieldEditor(nameof(PlaceModel.Description), true));
        //სტატუსი მისამართისაა (UrlModel.State) — რედაქტორი ჩანაწერის UrlNavigation-ზე მუშაობს და უმისამართო
        //ადგილს არ ეკითხება
        FieldEditors.Add(new PlaceStateFieldEditor(nameof(UrlModel.State), true));
        FieldEditors.Add(new LookupIdFieldEditor(nameof(PlaceModel.RegionId), "Region",
            () => travelGuideRepository.GetRegionsList().ToDictionary(k => k.RegionId, v => v.Name), true));
        FieldEditors.Add(new LookupIdFieldEditor(nameof(PlaceModel.MunicipalityId), "Municipality",
            () => travelGuideRepository.GetMunicipalitiesList().ToDictionary(k => k.MunicipalityId, v => v.Name),
            true));
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

    //ახალი ჩანაწერის მისამართი აქვე, სხვა ველებამდე იკითხება: Url ქროულერისთვის ჩანაწერის იდენტობაა და ველების
    //რედაქტორებში არ შედის — არსებულ ჩანაწერს ის მხოლოდ საჩვენებლად აქვს. მისამართი არასავალდებულოა — ცარიელი
    //Enter საიტზე არარსებულ, ხელით შესაყვან ადგილს ქმნის, რომელსაც ქროულერი არ ეხება. მითითებულ მისამართს ბოლო
    //დახრილი ხაზი ეჭრება და უნიკალურობა Urls ცხრილში ხეშ-კოდით მოწმდება ისევე, როგორც ქროულერის
    //HarvestedUrlPersister-ში; ახალი Urls-ის ჩანაწერი ადგილთან ერთად, ნავიგაციით ინახება
    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        while (true)
        {
            string? url = Inputer.InputText("Url", null)?.Trim().TrimEnd('/');
            if (string.IsNullOrEmpty(url))
            {
                return new PlaceModel();
            }

            if (url.Length > UrlModelConfiguration.UrlLength)
            {
                StShared.WriteErrorLine($"Url is too long (max {UrlModelConfiguration.UrlLength} characters)", true,
                    null, false);
                continue;
            }

            int urlHashCode = url.GetDeterministicHashCode();
            if (_travelGuideRepository.GetUrlIdsByUrlHashCode(urlHashCode).ContainsKey(url))
            {
                StShared.WriteErrorLine($"Url {url} already exists", true, null, false);
                continue;
            }

            return new PlaceModel { UrlNavigation = new UrlModel { Url = url, UrlHashCode = urlHashCode } };
        }
    }

    //ჩანაწერის მენიუში ველების რედაქტორების წინ მისამართი უცვლელი, საინფორმაციო პუნქტად ჩანს — მხოლოდ მაშინ,
    //როცა ადგილს მისამართი აქვს
    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string itemName)
    {
        if (_portion.TryGetValue(itemName, out ItemData? itemData) &&
            itemData is PlaceModel { UrlNavigation.Url: { } url })
        {
            itemSubMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Url", url));
        }

        base.FillDetailsSubMenu(itemSubMenuSet, itemName);
    }

    protected override ValueTask AddRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        //recordKey აქ შემთხვევითი Guid-ია (fieldKeyFromItem) — ჩანაწერს გასაღები (წარწერა) სიის ჩატვირთვისას
        //ენიჭება; მისამართი, თუ მითითებულია, CreateNewItem-მა უკვე შეამოწმა
        if (newRecord is not PlaceModel newPlace)
        {
            return ValueTask.CompletedTask;
        }

        _travelGuideRepository.AddPlace(newPlace);

        _travelGuideRepository.SaveChanges();
        return ValueTask.CompletedTask;
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
        //სტატუსი მისამართისაა და მხოლოდ მისამართიან ადგილს აქვს — ბმული მისამართი GetPlaceById-ს აქვს ჩატვირთული
        if (place.UrlNavigation is not null && newPlace.UrlNavigation is not null)
        {
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
