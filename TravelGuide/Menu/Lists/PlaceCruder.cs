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
using TravelGuideDbModels;
using TravelGuideDbPersistence.Configurations;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Lists;

//ადგილების (Places ცხრილის) რედაქტორი. ჩანაწერის გასაღები მისამართია (Url): ის უნიკალურია, ჩანაწერის
//შექმნისას ერთხელ იწერება და მერე აღარ იცვლება — ქროულერი ჩანაწერს სწორედ მისამართით (ხეშ-კოდით) ცნობს.
//fieldKeyFromItem=true — ცალკე Record Name ველი არ სჭირდება; მენიუში პუნქტის სახელად დასახელება გამოდის.
//ცხრილი ათასობით ჩანაწერს შეიცავს, ამიტომ ლექსიკონი მთელ ცხრილს კი არა, სიის ბოლოს ჩატვირთულ პორციას
//იჭერს (LoadPortion) — ჩანაწერის მენიუ (ველების რედაქტორები, თანმიმდევრობით რედაქტირება, წაშლა) მასზე
//მუშაობს და დასახელების შეცვლის შემდეგაც იმავე ასლს ხედავს, სანამ სია თავიდან არ ჩაიტვირთება
public sealed class PlaceCruder : Cruder
{
    private readonly ITravelGuideRepository _travelGuideRepository;

    //ბოლოს ჩატვირთული პორცია მისამართი-გასაღებებით
    private Dictionary<string, ItemData> _portion = new(StringComparer.Ordinal);

    public PlaceCruder(ITravelGuideRepository travelGuideRepository) : base("Place", "Places", true)
    {
        _travelGuideRepository = travelGuideRepository;
        FieldEditors.Add(new OptionalTextFieldEditor(nameof(PlaceModel.Name), true));
        FieldEditors.Add(new DescriptionFieldEditor(nameof(PlaceModel.Description), true));
        FieldEditors.Add(new PlaceStateFieldEditor(nameof(PlaceModel.State), true));
        FieldEditors.Add(new LookupIdFieldEditor(nameof(PlaceModel.RegionId), "Region",
            () => travelGuideRepository.GetRegionsList().ToDictionary(k => k.RegionId, v => v.Name), true));
        FieldEditors.Add(new LookupIdFieldEditor(nameof(PlaceModel.MunicipalityId), "Municipality",
            () => travelGuideRepository.GetMunicipalitiesList().ToDictionary(k => k.MunicipalityId, v => v.Name),
            true));
        //ლოკაციები ქვერედაქტორით იმართება — შექმნისა და თანმიმდევრობით რედაქტირებისას არ იკითხება
        FieldEditors.Add(new PlaceLocationsFieldEditor(nameof(PlaceModel.Locations), travelGuideRepository));
    }

    //სიის მიმდინარე პორციის ჩატვირთვა ფილტრით (დასახელების ან მისამართის ნაწილი). თითო ჩანაწერს მენიუს
    //პუნქტის სახელი (დასახელება, უსახელოსთვის მისამართი) ერთვის; გამეორებული სახელი რიგითი ნომრით
    //განსხვავდება — CliMenuSet.GetMenuItemWithName SingleOrDefault-ს იყენებს და გამეორებული სახელი
    //ბოლო ბრძანების გამეორებისას გამონაკლისს ისვრის
    public List<KeyValuePair<string, PlaceModel>> LoadPortion(string? filter, int skip, int take)
    {
        List<PlaceModel> places = _travelGuideRepository.GetPlacesPortion(filter, skip, take);
        _portion = places.ToDictionary(k => k.Url, ItemData (v) => v, StringComparer.Ordinal);

        var captionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        List<KeyValuePair<string, PlaceModel>> keyedPlaces = [];
        foreach (PlaceModel place in places)
        {
            string caption = string.IsNullOrWhiteSpace(place.Name) ? place.Url : place.Name;
            int seenCount = captionCounts.GetValueOrDefault(caption);
            captionCounts[caption] = seenCount + 1;
            if (seenCount > 0)
            {
                caption = string.Create(CultureInfo.InvariantCulture, $"{caption} ({seenCount + 1})");
            }

            keyedPlaces.Add(new KeyValuePair<string, PlaceModel>(caption, place));
        }

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

    //ახალი ჩანაწერის მისამართი აქვე, სხვა ველებამდე იკითხება: Url ჩანაწერის იდენტობაა და ველების
    //რედაქტორებში არ შედის — არსებულ ჩანაწერს ის მხოლოდ საჩვენებლად აქვს. ბოლო დახრილი ხაზი იჭრება და
    //უნიკალურობა ხეშ-კოდით მოწმდება ისევე, როგორც ქროულერის HarvestedUrlPersister-ში
    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        while (true)
        {
            string url = Inputer.InputTextRequired("Url").Trim().TrimEnd('/');
            if (url.Length == 0)
            {
                continue;
            }

            if (url.Length > PlaceModelConfiguration.UrlLength)
            {
                StShared.WriteErrorLine($"Url is too long (max {PlaceModelConfiguration.UrlLength} characters)", true,
                    null, false);
                continue;
            }

            int urlHashCode = url.GetDeterministicHashCode();
            if (_travelGuideRepository.GetPlaceIdsByUrlHashCode(urlHashCode).ContainsKey(url))
            {
                StShared.WriteErrorLine($"Place with Url {url} already exists", true, null, false);
                continue;
            }

            return new PlaceModel { Url = url, UrlHashCode = urlHashCode };
        }
    }

    //ჩანაწერის მენიუში ველების რედაქტორების წინ მისამართი უცვლელი, საინფორმაციო პუნქტად ჩანს
    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string itemName)
    {
        itemSubMenuSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand("Url", itemName));
        base.FillDetailsSubMenu(itemSubMenuSet, itemName);
    }

    protected override ValueTask AddRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        //recordKey აქ შემთხვევითი Guid-ია (fieldKeyFromItem) — ჩანაწერის გასაღები მისამართია, რომელიც
        //CreateNewItem-მა უკვე შეამოწმა
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
        place.State = newPlace.State;
        place.RegionId = newPlace.RegionId;
        place.MunicipalityId = newPlace.MunicipalityId;
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

        //ვიზიტებიანი ადგილი არ იშლება — კასკადი ვიზიტების ისტორიასაც წაშლიდა; ჯერ ვიზიტები უნდა წაიშალოს
        if (_travelGuideRepository.GetVisitCountsByPlaceIds([placeCopy.PlaceId]).GetValueOrDefault(placeCopy.PlaceId) >
            0)
        {
            StShared.WriteErrorLine($"Place {recordKey} has Visits and cannot be deleted. Delete its Visits first",
                true);
            return ValueTask.CompletedTask;
        }

        PlaceModel place = _travelGuideRepository.GetPlaceById(placeCopy.PlaceId) ??
                           throw new InvalidOperationException($"Place with id {placeCopy.PlaceId} not found");

        //ბმულების გრაფის წიბოები Restrict-ით არის მიბმული და ცალკე იშლება; დანარჩენი შვილობილი ჩანაწერები
        //(ლოკაციები, ტეგები, კატეგორიები, სეზონები, მანძილები) კასკადით მიჰყვება
        _travelGuideRepository.DeleteUrlGraphNodesByPlaceId(place.PlaceId);
        _travelGuideRepository.DeletePlace(place);

        _travelGuideRepository.SaveChanges();
        return ValueTask.CompletedTask;
    }
}
