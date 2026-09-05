using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using AppCliTools.CliMenu;
using SystemTools.SystemToolsShared;
using TravelGuide.Menu.Distances;
using TravelGuideDbModels;
using TravelGuideDbPersistence.Configurations;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Lists;

//მანძილების საწყისი წერტილების ცნობარის (FromPoints ცხრილის) რედაქტორი — ჩანაწერებს ადგილები DistanceByPlaces
//ჩანაწერებით ეყრდნობა. სახელები საიტიდან მოქაჩული ფორმითაა („თბილისიდან"). სახელის გარდა წერტილს
//არასავალდებულო მდებარეობა აქვს — Locations ცხრილის საზიარო ჩანაწერი (ერთი წყვილი ადგილებსაც შეიძლება
//ეკუთვნოდეს), ამიტომ შენახვისას არსებული წყვილი მეორდება ან ახალი იქმნება (GetOrCreateLocation), მოხსნისას
//კი მხოლოდ ბმული სუფთავდება — ობლად დარჩენილი ლოკაცია განზრახ რჩება, როგორც ადგილების ლოკაციების რედაქტორში.
//ჩანაწერის მენიუში ველების შემდეგ Calculate Distances პუნქტია — წერტილის მდებარეობიდან ადგილების ლოკაციებამდე
//მარშრუტების დათვლა RouteDistances ცხრილში (OSRM), ამიტომ რედაქტორს HttpClient-ის ქარხანა სჭირდება
public sealed class FromPointCruder : LookupCruder
{
    private readonly IHttpClientFactory _httpClientFactory;

    public FromPointCruder(ITravelGuideRepository travelGuideRepository, IHttpClientFactory httpClientFactory) : base(
        travelGuideRepository, "FromPoint", "FromPoints", FromPointModelConfiguration.NameLength)
    {
        _httpClientFactory = httpClientFactory;
        FieldEditors.Add(new OptionalLocationFieldEditor(nameof(FromPointItem.Location), true));
    }

    protected override List<LookupItem> LoadItems()
    {
        return
        [
            .. TravelGuideRepository.GetFromPointsList()
                .Select(s => new FromPointItem(s.FromPointId, s.Name, s.LocationNavigation))
        ];
    }

    protected override int? FindIdByName(string name)
    {
        return TravelGuideRepository.GetFromPointByName(name)?.FromPointId;
    }

    protected override ItemData CreateNewItem(string? recordKey, ItemData? defaultItemData)
    {
        return new FromPointItem(0, string.Empty, null);
    }

    protected override void Create(LookupItem item, string name)
    {
        //ქროულერის GetOrCreate გამოიყენება — სახელის უნიკალურობა უკვე შემოწმებულია და ახალი ჩანაწერი იქმნება
        SetLocation(TravelGuideRepository.GetOrCreateFromPoint(name), item);
    }

    protected override void Update(LookupItem item, string name)
    {
        FromPointModel fromPoint = GetFromPoint(item.Id);
        fromPoint.Name = name;
        SetLocation(fromPoint, item);
        TravelGuideRepository.UpdateFromPoint(fromPoint);
    }

    protected override int GetUsageCount(int id)
    {
        return TravelGuideRepository.GetPlacesCountByFromPointId(id);
    }

    protected override void Delete(int id)
    {
        TravelGuideRepository.DeleteFromPoint(GetFromPoint(id));
    }

    //ჩანაწერის მენიუში ველების რედაქტორების შემდეგ მანძილების გამოთვლის პუნქტი. მდებარეობა ჩანაწერის ასლიდან
    //იღება — მენიუ ყოველ გახსნაზე ბაზიდან თავიდან იტვირთება, ამიტომ ის მიმდინარეა; მდებარეობის გარეშე წერტილს
    //პუნქტი მაინც აქვს და გაშვებისას შეცდომას წერს
    public override void FillDetailsSubMenu(CliMenuSet itemSubMenuSet, string itemName)
    {
        base.FillDetailsSubMenu(itemSubMenuSet, itemName);

        if (GetItemByName(itemName, false) is not FromPointItem fromPointItem)
        {
            return;
        }

        LocationModel? startLocation = fromPointItem.Location is { } location
            ? new LocationModel
            {
                LocationId = location.LocationId, Latitude = location.Latitude, Longitude = location.Longitude
            }
            : null;
        itemSubMenuSet.AddMenuItem(new CalculateDistancesCommand(TravelGuideRepository, _httpClientFactory, itemName,
            startLocation));
    }

    //მდებარეობის მიბმა ან მოხსნა. ბმა ნავიგაციით იწერება, რადგან ახლადშექმნილ ლოკაციას იდენტიფიკატორი ჯერ
    //არ აქვს; მოხსნისას იდენტიფიკატორიც სუფთავდება, რადგან ბმული ლოკაცია კონტექსტში ჩატვირთული შეიძლება
    //არ იყოს და მარტო ნავიგაციის განულებას ცვლილებად ვერ დაინახავდა
    private void SetLocation(FromPointModel fromPoint, LookupItem item)
    {
        if (item is FromPointItem { Location: { } location })
        {
            fromPoint.LocationNavigation =
                TravelGuideRepository.GetOrCreateLocation(location.Latitude, location.Longitude);
            return;
        }

        fromPoint.LocationId = null;
        fromPoint.LocationNavigation = null;
    }

    //ბმული ჩანაწერი იდენტიფიკატორით — რედაქტორი მოუბმელ ასლებზე მუშაობს
    private FromPointModel GetFromPoint(int id)
    {
        return TravelGuideRepository.GetFromPointById(id) ??
               throw new InvalidOperationException($"FromPoint with id {id} not found");
    }
}
