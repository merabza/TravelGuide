using System;
using System.Collections.Generic;
using System.Linq;
using TravelGuideDbModels;
using TravelGuideDbPersistence.Configurations;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Lists;

//მანძილების საწყისი წერტილების ცნობარის (FromPoints ცხრილის) რედაქტორი — ჩანაწერებს ადგილები DistanceByPlaces
//ჩანაწერებით ეყრდნობა. სახელები საიტიდან მოქაჩული ფორმითაა („თბილისიდან")
public sealed class FromPointCruder : LookupCruder
{
    public FromPointCruder(ITravelGuideRepository travelGuideRepository) : base(travelGuideRepository, "FromPoint",
        "FromPoints", FromPointModelConfiguration.NameLength)
    {
    }

    protected override List<LookupItem> LoadItems()
    {
        return [.. TravelGuideRepository.GetFromPointsList().Select(s => new LookupItem(s.FromPointId, s.Name))];
    }

    protected override int? FindIdByName(string name)
    {
        return TravelGuideRepository.GetFromPointByName(name)?.FromPointId;
    }

    protected override void Create(string name)
    {
        //ქროულერის GetOrCreate გამოიყენება — სახელის უნიკალურობა უკვე შემოწმებულია და ახალი ჩანაწერი იქმნება
        TravelGuideRepository.GetOrCreateFromPoint(name);
    }

    protected override void Rename(int id, string name)
    {
        FromPointModel fromPoint = GetFromPoint(id);
        fromPoint.Name = name;
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

    //ბმული ჩანაწერი იდენტიფიკატორით — რედაქტორი მოუბმელ ასლებზე მუშაობს
    private FromPointModel GetFromPoint(int id)
    {
        return TravelGuideRepository.GetFromPointById(id) ??
               throw new InvalidOperationException($"FromPoint with id {id} not found");
    }
}
