using System;
using System.Collections.Generic;
using System.Linq;
using SystemTools.SystemToolsShared;
using TravelGuideDbModels;
using TravelGuideDbPersistence.Configurations;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Runners;

//შეგროვებული მისამართების ბაზაში ჩამოსატვირთი (New) სტატუსით შენახვა — საერთოა Selenium-ით, sitemap-ით
//და გვერდების გაანალიზებისას ბმულების ამოკრებით შეგროვებისთვის. ინახება მხოლოდ საწყისი წერტილების
//მსგავსი მისამართები: ზუსტად საწყისი წერტილი ან მისი ქვეგვერდი
public sealed class HarvestedUrlPersister
{
    //ბაზაში უკვე არსებული მისამართები ერთხელ იტვირთება და შენახვისას ივსება, რომ ბაზის განმეორებითი კითხვა არ დაჭირდეს
    private readonly HashSet<string> _knownUrls;
    private readonly ITravelGuideRepository _repository;

    //საწყისი წერტილები ბოლო „/"-ის გარეშე და პრეფიქსად გამოსაყენებელი ფორმით
    private readonly List<(string Exact, string Prefix)> _startPointPatterns;

    public HarvestedUrlPersister(ITravelGuideRepository repository, IEnumerable<string> startPoints)
    {
        _repository = repository;
        _startPointPatterns = [.. startPoints.Select(s => s.Trim().TrimEnd('/')).Select(s => (s, s + "/"))];
        _knownUrls = new HashSet<string>(repository.GetAllPlaceUrls(), StringComparer.Ordinal);
    }

    public int PersistNewUrls(IReadOnlyCollection<string> urlList)
    {
        var newCount = 0;

        //ბოლო „/" იჭრება, რომ ერთი და იგივე გვერდი ორი ფორმით არ შეინახოს; HashSet გამეორებებსაც ფილტრავს
        foreach (string url in urlList.Select(s => s.TrimEnd('/')).Where(IsLikeStartPoint).Where(_knownUrls.Add))
        {
            if (url.Length > PlaceModelConfiguration.UrlLength)
            {
                StShared.WriteErrorLine($"Url is too long and will be skipped: {url}", true, null, false);
                continue;
            }

            _repository.AddPlace(new PlaceModel { Url = url, State = EState.New });
            newCount++;
        }

        if (newCount > 0)
        {
            _repository.SaveChanges();
            Console.WriteLine($"Checked {urlList.Count} urls, new: {newCount}");
        }

        return newCount;
    }

    private bool IsLikeStartPoint(string url)
    {
        return _startPointPatterns.Exists(p =>
            url.Equals(p.Exact, StringComparison.Ordinal) || url.StartsWith(p.Prefix, StringComparison.Ordinal));
    }
}
