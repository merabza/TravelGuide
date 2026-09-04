using System;
using System.Collections.Generic;
using System.Linq;
using TravelGuideDbModels;
using TravelGuideDbPersistence.Configurations;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Lists;

//კატეგორიების ცნობარის (Categories ცხრილის) რედაქტორი — ჩანაწერებს ადგილები PlacesByCategories ბმულებით
//ეყრდნობა
public sealed class CategoryCruder : LookupCruder
{
    public CategoryCruder(ITravelGuideRepository travelGuideRepository) : base(travelGuideRepository, "Category",
        "Categories", CategoryModelConfiguration.NameLength)
    {
    }

    protected override List<LookupItem> LoadItems()
    {
        return [.. TravelGuideRepository.GetCategoriesList().Select(s => new LookupItem(s.CategoryId, s.Name))];
    }

    protected override int? FindIdByName(string name)
    {
        return TravelGuideRepository.GetCategoryByName(name)?.CategoryId;
    }

    protected override void Create(string name)
    {
        //ქროულერის GetOrCreate გამოიყენება — სახელის უნიკალურობა უკვე შემოწმებულია და ახალი ჩანაწერი იქმნება
        TravelGuideRepository.GetOrCreateCategory(name);
    }

    protected override void Rename(int id, string name)
    {
        CategoryModel category = GetCategory(id);
        category.Name = name;
        TravelGuideRepository.UpdateCategory(category);
    }

    protected override int GetUsageCount(int id)
    {
        return TravelGuideRepository.GetPlacesCountByCategoryId(id);
    }

    protected override void Delete(int id)
    {
        TravelGuideRepository.DeleteCategory(GetCategory(id));
    }

    //ბმული ჩანაწერი იდენტიფიკატორით — რედაქტორი მოუბმელ ასლებზე მუშაობს
    private CategoryModel GetCategory(int id)
    {
        return TravelGuideRepository.GetCategoryById(id) ??
               throw new InvalidOperationException($"Category with id {id} not found");
    }
}
