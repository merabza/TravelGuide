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

    //ბაზაში უკვე არსებული (FromUrlId, GotUrlId) წყვილები — ერთი და იგივე კავშირი მეორედ არ შეინახოს
    private readonly HashSet<(int FromUrlId, int GotUrlId)> _knownUrlPairs;

    private readonly ITravelGuideRepository _repository;

    //საწყისი წერტილები ბოლო „/"-ის გარეშე და პრეფიქსად გამოსაყენებელი ფორმით
    private readonly List<(string Exact, string Prefix)> _startPointPatterns;

    //მისამართი -> PlaceId, UrlGraphNodes-ის კავშირებისთვის; ახლად შენახულების იდენტიფიკატორები SaveChanges-ის შემდეგ ემატება
    private readonly Dictionary<string, int> _urlIds;

    public HarvestedUrlPersister(ITravelGuideRepository repository, IEnumerable<string> startPoints)
    {
        _repository = repository;
        _startPointPatterns = [.. startPoints.Select(s => s.Trim().TrimEnd('/')).Select(s => (s, s + "/"))];
        _urlIds = repository.GetPlaceIdsByUrl();
        _knownUrls = new HashSet<string>(_urlIds.Keys, StringComparer.Ordinal);
        _knownUrlPairs = [.. repository.GetAllUrlGraphNodes().Select(s => (s.FromUrlId, s.GotUrlId))];
    }

    //fromUrl იმ გვერდის მისამართია, სადაც urlList მოიძებნა — მითითებისას ნაპოვნი კავშირები UrlGraphNodes-შიც ინახება.
    //საწყისი წერტილების ჩარიგებას და sitemap-ს წყარო გვერდი არ აქვს და fromUrl-ს არ გადმოსცემს
    public int PersistNewUrls(IReadOnlyCollection<string> urlList, string? fromUrl = null)
    {
        var newCount = 0;
        List<PlaceModel> addedPlaces = [];

        //ბოლო „/" იჭრება, რომ ერთი და იგივე გვერდი ორი ფორმით არ შეინახოს; HashSet გამეორებებსაც ფილტრავს
        foreach (string url in urlList.Select(s => s.TrimEnd('/')).Where(IsLikeStartPoint).Where(_knownUrls.Add))
        {
            if (url.Length > PlaceModelConfiguration.UrlLength)
            {
                StShared.WriteErrorLine($"Url is too long and will be skipped: {url}", true, null, false);
                continue;
            }

            addedPlaces.Add(_repository.AddPlace(new PlaceModel { Url = url, State = EState.New }));
            newCount++;
        }

        if (newCount > 0)
        {
            _repository.SaveChanges();
            Console.WriteLine($"Checked {urlList.Count} urls, new: {newCount}");

            //SaveChanges-ის შემდეგ ახალ ჩანაწერებს იდენტიფიკატორები აქვს მინიჭებული და კავშირებში გამოყენებადია
            foreach (PlaceModel place in addedPlaces)
            {
                _urlIds[place.Url] = place.PlaceId;
            }
        }

        if (fromUrl is not null)
        {
            PersistUrlGraphNodes(fromUrl, urlList);
        }

        return newCount;
    }

    //რომელ გვერდზე რომელი მისამართი მოიძებნა — გრაფის კავშირების შენახვა. მხოლოდ Places-ში არსებულ
    //მისამართებს შორის: ფილტრში ჩაჭრილი ან ზღვარგადაცილებული მისამართები ბაზაში არ არის და კავშირიც არ ჩაიწერება
    private void PersistUrlGraphNodes(string fromUrl, IReadOnlyCollection<string> urlList)
    {
        if (!_urlIds.TryGetValue(fromUrl.TrimEnd('/'), out int fromUrlId))
        {
            return;
        }

        var newPairsCount = 0;
        foreach (string url in urlList.Select(s => s.TrimEnd('/')).Where(IsLikeStartPoint))
        {
            //გვერდის ბმული საკუთარ თავზე გრაფისთვის აზრს მოკლებულია და გამოიტოვება
            if (!_urlIds.TryGetValue(url, out int gotUrlId) || gotUrlId == fromUrlId ||
                !_knownUrlPairs.Add((fromUrlId, gotUrlId)))
            {
                continue;
            }

            _repository.AddUrlGraphNode(new UrlGraphNode { FromUrlId = fromUrlId, GotUrlId = gotUrlId });
            newPairsCount++;
        }

        if (newPairsCount > 0)
        {
            _repository.SaveChanges();
            Console.WriteLine($"New url graph nodes: {newPairsCount}");
        }
    }

    private bool IsLikeStartPoint(string url)
    {
        return _startPointPatterns.Exists(p =>
            url.Equals(p.Exact, StringComparison.Ordinal) || url.StartsWith(p.Prefix, StringComparison.Ordinal));
    }
}
