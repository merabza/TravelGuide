using System;
using System.Collections.Generic;
using System.Linq;
using TravelGuideCore.Domain.RegionModels;
using TravelGuideDbPart.Db.Configurations;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Lists;

//რეგიონების ცნობარის (Regions ცხრილის) რედაქტორი — ჩანაწერებს ადგილები RegionId-ით ეყრდნობა
public sealed class RegionCruder : LookupCruder
{
    public RegionCruder(ITravelGuideRepository travelGuideRepository) : base(travelGuideRepository, "Region", "Regions",
        RegionModelConfiguration.NameLength)
    {
    }

    protected override List<LookupItem> LoadItems()
    {
        return [.. TravelGuideRepository.GetRegionsList().Select(s => new LookupItem(s.RegionId, s.Name))];
    }

    protected override int? FindIdByName(string name)
    {
        return TravelGuideRepository.GetRegionByName(name)?.RegionId;
    }

    protected override void Create(LookupItem item, string name)
    {
        //ქროულერის GetOrCreate გამოიყენება — სახელის უნიკალურობა უკვე შემოწმებულია და ახალი ჩანაწერი იქმნება
        TravelGuideRepository.GetOrCreateRegion(name);
    }

    protected override void Update(LookupItem item, string name)
    {
        RegionModel region = GetRegion(item.Id);
        region.Name = name;
        TravelGuideRepository.UpdateRegion(region);
    }

    protected override int GetUsageCount(int id)
    {
        return TravelGuideRepository.GetPlacesCountByRegionId(id);
    }

    protected override void Delete(int id)
    {
        TravelGuideRepository.DeleteRegion(GetRegion(id));
    }

    //ბმული ჩანაწერი იდენტიფიკატორით — რედაქტორი მოუბმელ ასლებზე მუშაობს
    private RegionModel GetRegion(int id)
    {
        return TravelGuideRepository.GetRegionById(id) ??
               throw new InvalidOperationException($"Region with id {id} not found");
    }
}
