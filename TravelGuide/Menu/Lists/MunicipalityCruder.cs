using System;
using System.Collections.Generic;
using System.Linq;
using TravelGuideDbModels;
using TravelGuideDbPersistence.Configurations;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Lists;

//მუნიციპალიტეტების ცნობარის (Municipalities ცხრილის) რედაქტორი — ჩანაწერებს ადგილები MunicipalityId-ით
//ეყრდნობა
public sealed class MunicipalityCruder : LookupCruder
{
    public MunicipalityCruder(ITravelGuideRepository travelGuideRepository) : base(travelGuideRepository,
        "Municipality", "Municipalities", MunicipalityModelConfiguration.NameLength)
    {
    }

    protected override List<LookupItem> LoadItems()
    {
        return
        [
            .. TravelGuideRepository.GetMunicipalitiesList().Select(s => new LookupItem(s.MunicipalityId, s.Name))
        ];
    }

    protected override int? FindIdByName(string name)
    {
        return TravelGuideRepository.GetMunicipalityByName(name)?.MunicipalityId;
    }

    protected override void Create(LookupItem item, string name)
    {
        //ქროულერის GetOrCreate გამოიყენება — სახელის უნიკალურობა უკვე შემოწმებულია და ახალი ჩანაწერი იქმნება
        TravelGuideRepository.GetOrCreateMunicipality(name);
    }

    protected override void Update(LookupItem item, string name)
    {
        MunicipalityModel municipality = GetMunicipality(item.Id);
        municipality.Name = name;
        TravelGuideRepository.UpdateMunicipality(municipality);
    }

    protected override int GetUsageCount(int id)
    {
        return TravelGuideRepository.GetPlacesCountByMunicipalityId(id);
    }

    protected override void Delete(int id)
    {
        TravelGuideRepository.DeleteMunicipality(GetMunicipality(id));
    }

    //ბმული ჩანაწერი იდენტიფიკატორით — რედაქტორი მოუბმელ ასლებზე მუშაობს
    private MunicipalityModel GetMunicipality(int id)
    {
        return TravelGuideRepository.GetMunicipalityById(id) ??
               throw new InvalidOperationException($"Municipality with id {id} not found");
    }
}
