using System;
using System.Collections.Generic;
using System.Linq;
using TravelGuideCore.Domain.TagModels;
using TravelGuideDbPart.Db.Configurations;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Lists;

//ტეგების ცნობარის (Tags ცხრილის) რედაქტორი — ჩანაწერებს ადგილები PlacesByTags ბმულებით ეყრდნობა
public sealed class TagCruder : LookupCruder
{
    public TagCruder(ITravelGuideRepository travelGuideRepository) : base(travelGuideRepository, "Tag", "Tags",
        TagModelConfiguration.NameLength)
    {
    }

    protected override List<LookupItem> LoadItems()
    {
        return [.. TravelGuideRepository.GetTagsList().Select(s => new LookupItem(s.TagId, s.Name))];
    }

    protected override int? FindIdByName(string name)
    {
        return TravelGuideRepository.GetTagByName(name)?.TagId;
    }

    protected override void Create(LookupItem item, string name)
    {
        //ქროულერის GetOrCreate გამოიყენება — სახელის უნიკალურობა უკვე შემოწმებულია და ახალი ჩანაწერი იქმნება
        TravelGuideRepository.GetOrCreateTag(name);
    }

    protected override void Update(LookupItem item, string name)
    {
        TagModel tag = GetTag(item.Id);
        tag.Name = name;
        TravelGuideRepository.UpdateTag(tag);
    }

    protected override int GetUsageCount(int id)
    {
        return TravelGuideRepository.GetPlacesCountByTagId(id);
    }

    protected override void Delete(int id)
    {
        TravelGuideRepository.DeleteTag(GetTag(id));
    }

    //ბმული ჩანაწერი იდენტიფიკატორით — რედაქტორი მოუბმელ ასლებზე მუშაობს
    private TagModel GetTag(int id)
    {
        return TravelGuideRepository.GetTagById(id) ??
               throw new InvalidOperationException($"Tag with id {id} not found");
    }
}
