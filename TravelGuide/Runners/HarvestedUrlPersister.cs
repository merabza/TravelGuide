using System;
using System.Collections.Generic;
using System.Linq;
using SystemTools.SystemToolsShared;
using TravelGuideDbModels;
using TravelGuideDbPersistence.Configurations;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Runners;

//შეგროვებული მისამართების ბაზაში შენახვა საერთოა Selenium-ით და sitemap-ით შეგროვებისთვის
public static class HarvestedUrlPersister
{
    public static void PersistNewUrls(ITravelGuideRepository repository, IReadOnlyCollection<string> urlList)
    {
        //ბაზაში უკვე არსებული მისამართები აღარ ემატება; HashSet ერთი შეგროვების შიგნით გამეორებებსაც ფილტრავს
        var known = new HashSet<string>(repository.GetAllPlaceUrls(), StringComparer.Ordinal);
        var newCount = 0;
        foreach (var url in urlList.Where(known.Add))
        {
            if (url.Length > PlaceModelConfiguration.UrlLength)
            {
                StShared.WriteErrorLine($"Url is too long and will be skipped: {url}", true, null, false);
                continue;
            }

            repository.AddPlace(new PlaceModel { Url = url, State = EState.New });
            newCount++;
        }

        if (newCount > 0)
        {
            repository.SaveChanges();
        }

        Console.WriteLine($"Collected {urlList.Count} urls, new: {newCount}");
    }
}
