using System;
using System.Collections.Generic;
using System.Linq;
using TravelGuideDbModels;
using TravelGuideDbPersistence.Configurations;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Runners;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class PlaceLinksSynchronizer
{
    private readonly ITravelGuideRepository _repository;

    public PlaceLinksSynchronizer(ITravelGuideRepository repository)
    {
        _repository = repository;
    }

    //თორმეტივე თვის ჩანაწერი ანალიზამდე ერთხელ მზადდება, რომ ბმულებმა MonthId რეალური გასაღებით გამოიყენონ
    public void EnsureMonths()
    {
        List<MonthModel> existingMonths = _repository.GetMonths();
        var addedCount = 0;
        for (var monthId = 1; monthId <= BestSeasonParser.MonthNames.Count; monthId++)
        {
            if (existingMonths.Exists(e => e.MonthId == monthId))
            {
                continue;
            }

            _repository.AddMonth(new MonthModel { MonthId = monthId, Name = BestSeasonParser.MonthNames[monthId - 1] });
            addedCount++;
        }

        if (addedCount > 0)
        {
            _repository.SaveChanges();
        }
    }

    public void SyncPlaceLinks(PlaceExtractResult extract, PlaceModel place)
    {
        //ჯერ ყველა საჭირო ჩანაწერი მოიძებნება ან იქმნება და place მხოლოდ ამის შემდეგ იცვლება,
        //რომ შეცდომისას ნახევრად შეცვლილი ბმულები მომდევნო SaveChanges-ში არ გაიპაროს
        List<int> monthIds = BestSeasonParser.ParseMonths(extract.BestSeason);
        List<CategoryModel> categories =
        [
            .. NormalizeNames(extract.Categories, CategoryModelConfiguration.NameLength)
                .Select(_repository.GetOrCreateCategory)
        ];
        List<TagModel> tags =
            [.. NormalizeNames(extract.Tags, TagModelConfiguration.NameLength).Select(_repository.GetOrCreateTag)];
        List<(FromPointModel FromPoint, int DistanceKm)> distances =
        [
            .. NormalizeDistances(extract.Distances)
                .Select(s => (_repository.GetOrCreateFromPoint(s.FromPointName), s.DistanceKm))
        ];

        SyncBestSeasons(place, monthIds);
        SyncCategories(place, categories);
        SyncTags(place, tags);
        SyncDistances(place, distances);
    }

    private static List<string> NormalizeNames(IEnumerable<string> names, int maxLength)
    {
        return
        [
            .. names.Select(s => s.Trim()).Where(w => w.Length > 0)
                .Select(s => s.Length <= maxLength ? s : s[..maxLength]).Distinct(StringComparer.Ordinal)
        ];
    }

    //ერთ საწყის წერტილზე გვერდზე რამდენიმე ჩანაწერიდან პირველი რჩება, რომ უნიკალურ წყვილს ორი მნიშვნელობა არ შეეჯახოს
    private static List<PlaceDistanceItem> NormalizeDistances(IEnumerable<PlaceDistanceItem> distances)
    {
        return
        [
            .. distances.Select(s => s with { FromPointName = s.FromPointName.Trim() })
                .Where(w => w.FromPointName.Length > 0)
                .Select(s => s.FromPointName.Length <= FromPointModelConfiguration.NameLength
                    ? s
                    : s with { FromPointName = s.FromPointName[..FromPointModelConfiguration.NameLength] })
                .DistinctBy(d => d.FromPointName, StringComparer.Ordinal)
        ];
    }

    //განახლება სხვაობით ხდება და არა Clear-ით: ერთ კონტექსტში წაშლილად მონიშნული და იმავე გასაღებით
    //ხელახლა დამატებული ბმული EF-ს identity-კონფლიქტში აგდებს
    private static void SyncBestSeasons(PlaceModel place, List<int> monthIds)
    {
        List<PlaceByBestSeason> linksToRemove = [.. place.BestSeasons.Where(w => !monthIds.Contains(w.MonthId))];
        foreach (PlaceByBestSeason link in linksToRemove)
        {
            place.BestSeasons.Remove(link);
        }

        foreach (int monthId in monthIds.Where(w => place.BestSeasons.All(a => a.MonthId != w)))
        {
            place.BestSeasons.Add(new PlaceByBestSeason { MonthId = monthId });
        }
    }

    //შედარება ობიექტების იგივეობით ხდება და არა Id-ით: ახალშექმნილ ჩანაწერებს რეალური გასაღები ჯერ არ აქვთ,
    //ერთი კონტექსტის identity map კი ერთ მწკრივს ყოველთვის ერთ ობიექტს შეუსაბამებს
    private static void SyncCategories(PlaceModel place, List<CategoryModel> categories)
    {
        List<PlaceByCategory> linksToRemove =
            [.. place.Categories.Where(w => !categories.Contains(w.CategoryNavigation))];
        foreach (PlaceByCategory link in linksToRemove)
        {
            place.Categories.Remove(link);
        }

        foreach (CategoryModel category in categories.Where(w =>
                     !place.Categories.Any(a => ReferenceEquals(a.CategoryNavigation, w))))
        {
            place.Categories.Add(new PlaceByCategory { CategoryNavigation = category });
        }
    }

    private static void SyncTags(PlaceModel place, List<TagModel> tags)
    {
        List<PlaceByTag> linksToRemove = [.. place.Tags.Where(w => !tags.Contains(w.TagNavigation))];
        foreach (PlaceByTag link in linksToRemove)
        {
            place.Tags.Remove(link);
        }

        foreach (TagModel tag in tags.Where(w => !place.Tags.Any(a => ReferenceEquals(a.TagNavigation, w))))
        {
            place.Tags.Add(new PlaceByTag { TagNavigation = tag });
        }
    }

    //მანძილი ბმულის ატრიბუტია: არსებულ წყვილს მხოლოდ Distance უახლდება, გამქრალი წყვილები კი იშლება
    private static void SyncDistances(PlaceModel place, List<(FromPointModel FromPoint, int DistanceKm)> distances)
    {
        List<DistanceByPlace> linksToRemove =
        [
            .. place.Distances.Where(w => !distances.Any(a => ReferenceEquals(a.FromPoint, w.FromPointNavigation)))
        ];
        foreach (DistanceByPlace link in linksToRemove)
        {
            place.Distances.Remove(link);
        }

        foreach ((FromPointModel fromPoint, int distanceKm) in distances)
        {
            DistanceByPlace? existing =
                place.Distances.FirstOrDefault(f => ReferenceEquals(f.FromPointNavigation, fromPoint));
            if (existing is null)
            {
                place.Distances.Add(new DistanceByPlace { FromPointNavigation = fromPoint, Distance = distanceKm });
            }
            else
            {
                existing.Distance = distanceKm;
            }
        }
    }
}
