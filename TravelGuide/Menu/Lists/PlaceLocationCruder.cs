using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.Cruders;
using AppCliTools.CliParameters.FieldEditors;
using SystemTools.SystemToolsShared;
using TravelGuide.Runners;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Lists;

//ერთი ადგილის ლოკაციების (PlacesByLocations ბმულების) რედაქტორი VisitImageCruder-ის ყაიდაზე. ჩანაწერის
//გასაღები კოორდინატებია („განედი, გრძედი") და შეცვლისას თვითონაც იცვლება, ამიტომ ჩანაწერის მენიუში
//ცალკეული ველების რედაქტორები არ ჩანს (FillDetailsSubMenu) — ველის შეცვლის შემდეგ მენიუ ძველი
//გასაღებით გადაიწყობოდა და ჩანაწერს ვეღარ იპოვიდა; „Edit All fields in sequence" კი წარმატებისას
//სიაზე ბრუნდება, რომელიც ბაზიდან თავიდან იტვირთება.
//ლოკაცია საზიარო ჩანაწერია (ერთი წყვილი რამდენიმე ადგილს შეიძლება ეკუთვნოდეს), ამიტომ ჩამატება
//არსებულს იყენებს ან ახალს ქმნის (GetOrCreateLocation), წაშლა კი მხოლოდ ბმულს ხსნის — ობლად დარჩენილი
//ლოკაცია განზრახ რჩება, როგორც ქროულერის PlaceLinksSynchronizer.SyncLocations-ში
public sealed class PlaceLocationCruder : Cruder
{
    private readonly int _placeId;
    private readonly ITravelGuideRepository _travelGuideRepository;

    //fieldKeyFromItem=true — ლოკაციას რედაქტირებადი სახელი არ აქვს და Record Name ველი არ სჭირდება
    public PlaceLocationCruder(ITravelGuideRepository travelGuideRepository, int placeId) : base("Location",
        "Locations", true)
    {
        _travelGuideRepository = travelGuideRepository;
        _placeId = placeId;
        FieldEditors.Add(new DoubleFieldEditor(nameof(LocationItem.Latitude), 0, true));
        FieldEditors.Add(new DoubleFieldEditor(nameof(LocationItem.Longitude), 0, true));
    }

    protected override Dictionary<string, ItemData> GetCrudersDictionary()
    {
        try
        {
            //ბაზაში (Latitude, Longitude) უნიკალურია და ერთი ლოკაცია ადგილს ერთხელ ებმება, ამიტომ
            //კოორდინატების გასაღებები არ მეორდება
            return _travelGuideRepository.GetPlaceLocations(_placeId)
                .Select(s => new LocationItem(s.LocationNavigation))
                .ToDictionary(k => k.GetItemKey(), ItemData (v) => v, StringComparer.Ordinal);
        }
        catch (Exception e)
        {
            //ბაზასთან დაკავშირება ვერ მოხერხდა — მენიუ ცარიელი სიით აეწყობა
            StShared.WriteException(e, true);
            return [];
        }
    }

    public override bool ContainsRecordWithKey(string recordKey)
    {
        return GetCrudersDictionary().ContainsKey(recordKey);
    }

    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        return new LocationItem();
    }

    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string itemName)
    {
        //საბაზო რეალიზაცია განზრახ არ იძახება — ცალკეული ველის რედაქტორი გასაღებს შეცვლიდა (იხ. კლასის კომენტარი)
    }

    protected override ValueTask AddRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        //recordKey აქ შემთხვევითი Guid-ია (fieldKeyFromItem) — ჩანაწერის გასაღები კოორდინატებია
        if (newRecord is not LocationItem newItem || !CheckCoordinates(newItem))
        {
            return ValueTask.CompletedTask;
        }

        LocationModel? location = GetOrCreateUnlinkedLocation(newItem);
        if (location is null)
        {
            return ValueTask.CompletedTask;
        }

        _travelGuideRepository.AddPlaceLocation(_placeId, location);

        _travelGuideRepository.SaveChanges();
        return ValueTask.CompletedTask;
    }

    public override ValueTask UpdateRecordWithKey(string recordKey, ItemData newRecord,
        CancellationToken cancellationToken = default)
    {
        //კოორდინატები არ შეცვლილა — გასაღები იგივე დარჩა და შესანახი არაფერია
        if (newRecord is not LocationItem newItem || newItem.GetItemKey() == recordKey ||
            !CheckCoordinates(newItem))
        {
            return ValueTask.CompletedTask;
        }

        //ლოკაციის კოორდინატები უცვლელია (უნიკალური ინდექსით), ამიტომ შეცვლა ძველი ბმულის მოხსნა და
        //ახალი — არსებული ან ახლადშექმნილი — ლოკაციის მიბმაა. ძველი ბმული ჯერ მოწმდება, რომ ახალი
        //ლოკაცია კონტექსტში ტყუილად არ შეიქმნას
        PlaceByLocation oldLink = _travelGuideRepository.GetPlaceLocation(_placeId, newItem.LocationId) ??
                                  throw new InvalidOperationException($"Location with key {recordKey} not found");

        LocationModel? location = GetOrCreateUnlinkedLocation(newItem);
        if (location is null)
        {
            return ValueTask.CompletedTask;
        }

        _travelGuideRepository.DeletePlaceLocation(oldLink);
        _travelGuideRepository.AddPlaceLocation(_placeId, location);

        _travelGuideRepository.SaveChanges();
        return ValueTask.CompletedTask;
    }

    protected override ValueTask RemoveRecordWithKey(string recordKey, CancellationToken cancellationToken = default)
    {
        if (!GetCrudersDictionary().TryGetValue(recordKey, out ItemData? itemData) ||
            itemData is not LocationItem item)
        {
            throw new InvalidOperationException($"Location with key {recordKey} not found");
        }

        PlaceByLocation link = _travelGuideRepository.GetPlaceLocation(_placeId, item.LocationId) ??
                               throw new InvalidOperationException($"Location with key {recordKey} not found");
        _travelGuideRepository.DeletePlaceLocation(link);

        _travelGuideRepository.SaveChanges();
        return ValueTask.CompletedTask;
    }

    //კოორდინატები გეოგრაფიულ ზღვრებში უნდა იყოს — იმავე შემოწმებით, რომლითაც ქროულერი გვერდიდან
    //წაკითხულს ფილტრავს
    private static bool CheckCoordinates(LocationItem item)
    {
        if (PlaceDataExtractor.IsValidCoordinatePair(item.Latitude, item.Longitude))
        {
            return true;
        }

        StShared.WriteErrorLine(
            $"Invalid coordinates {item.GetItemKey()}: latitude must be within [-90, 90] and longitude within [-180, 180]",
            true);
        return false;
    }

    //ლოკაცია არსებულთაგან მოიძებნება ან იქმნება; ამ ადგილზე უკვე მიბმულისთვის შეცდომა იწერება და null
    //ბრუნდება. ახლადშექმნილს გასაღები ჯერ არ აქვს (LocationId=0) და მიბმული ვერ იქნება
    private LocationModel? GetOrCreateUnlinkedLocation(LocationItem item)
    {
        LocationModel location = _travelGuideRepository.GetOrCreateLocation(item.Latitude, item.Longitude);
        if (location.LocationId == 0 ||
            _travelGuideRepository.GetPlaceLocation(_placeId, location.LocationId) is null)
        {
            return location;
        }

        StShared.WriteErrorLine($"Location {item.GetItemKey()} is already linked to this place", true);
        return null;
    }
}
