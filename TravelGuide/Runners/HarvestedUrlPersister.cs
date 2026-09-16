using System;
using System.Collections.Generic;
using System.Linq;
using SystemTools.SystemToolsShared;
using TravelGuideCore.Domain;
using TravelGuideCore.Domain.PlaceModels;
using TravelGuideCore.Domain.UrlGraphNodes;
using TravelGuideCore.Domain.UrlModels;
using TravelGuideDbPart.Db.Configurations;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Runners;

//შეგროვებული მისამართების ბაზაში შენახვა: თითო ახალი მისამართი Urls ცხრილის ჩამოსატვირთი (New) სტატუსის
//ჩანაწერია და მასზე მიბმული ადგილი — საერთოა Selenium-ით, sitemap-ით და გვერდების გაანალიზებისას ბმულების
//ამოკრებით შეგროვებისთვის. ინახება მხოლოდ საწყისი წერტილების მსგავსი მისამართები: ზუსტად საწყისი წერტილი
//ან მისი ქვეგვერდი
public sealed class HarvestedUrlPersister
{
    //ბაზაში უკვე არსებული (FromUrlId, GotUrlId) წყვილები — ერთი და იგივე კავშირი მეორედ არ შეინახოს
    private readonly HashSet<(int FromUrlId, int GotUrlId)> _knownUrlPairs;

    //ამ გაშვებაში უკვე ნანახი მისამართები — თითო მისამართი ბაზაში ხეშ-კოდით მხოლოდ ერთხელ შემოწმდეს
    private readonly HashSet<string> _knownUrls = new(StringComparer.Ordinal);

    private readonly ITravelGuideRepository _repository;

    //საწყისი წერტილები ბოლო „/"-ის გარეშე და პრეფიქსად გამოსაყენებელი ფორმით
    private readonly List<(string Exact, string Prefix)> _startPointPatterns;

    //მისამართი -> UrlId, UrlGraphNodes-ის კავშირებისთვის; ბაზაში ნაპოვნი და ახლად შენახული მისამართების
    //იდენტიფიკატორებით თანდათან ივსება, რომ ბაზის განმეორებითი კითხვა არ დაჭირდეს
    private readonly Dictionary<string, int> _urlIds = new(StringComparer.Ordinal);

    public HarvestedUrlPersister(ITravelGuideRepository repository, IEnumerable<string> startPoints)
    {
        _repository = repository;
        _startPointPatterns = [.. startPoints.Select(s => s.Trim().TrimEnd('/')).Select(s => (s, s + "/"))];
        _knownUrlPairs = [.. repository.GetAllUrlGraphNodes().Select(s => (s.FromUrlId, s.GotUrlId))];
    }

    //fromUrl იმ გვერდის მისამართია, სადაც urlList მოიძებნა — მითითებისას ნაპოვნი კავშირები UrlGraphNodes-შიც ინახება.
    //საწყისი წერტილების ჩარიგებას და sitemap-ს წყარო გვერდი არ აქვს და fromUrl-ს არ გადმოსცემს
    public int PersistNewUrls(IReadOnlyCollection<string> urlList, string? fromUrl = null)
    {
        var newCount = 0;
        List<(string Url, UrlModel UrlModel)> addedUrls = [];

        //ბოლო „/" იჭრება, რომ ერთი და იგივე გვერდი ორი ფორმით არ შეინახოს; HashSet გამეორებებსაც ფილტრავს
        foreach (string url in urlList.Select(s => s.TrimEnd('/')).Where(IsLikeStartPoint).Where(_knownUrls.Add))
        {
            if (url.Length > UrlModelConfiguration.UrlLength)
            {
                StShared.WriteErrorLine($"Url is too long and will be skipped: {url}", true, null, false);
                continue;
            }

            //Url ბაზაში არ ინდექსირდება და უნიკალურობას აპლიკაცია იცავს: შენახვამდე ითვლება მისამართის
            //ხეშ-კოდი, Urls-იდან ამოიკრიბება იგივე ხეშის მქონე ჩანაწერები და ზუსტი შედარებით მოწმდება,
            //რომ ეს მისამართი უკვე შენახული არ არის
            int urlHashCode = url.GetDeterministicHashCode();
            if (_repository.GetUrlIdsByUrlHashCode(urlHashCode).TryGetValue(url, out int existingUrlId))
            {
                _urlIds[url] = existingUrlId;
                continue;
            }

            //ახალი მისამართი ჩამოსატვირთი (New) სტატუსით და მასზე მიბმული ადგილი ერთად იქმნება — Urls-ის ჩანაწერი
            //ადგილის ნავიგაციით იწერება და შენახვისას UrlId ივსება
            var newUrl = new UrlModel { Url = url, UrlHashCode = urlHashCode, State = EState.New };
            _repository.AddPlace(new PlaceModel { UrlNavigation = newUrl });
            addedUrls.Add((url, newUrl));
            newCount++;
        }

        if (newCount > 0)
        {
            _repository.SaveChanges();
            Console.WriteLine($"Checked {urlList.Count} urls, new: {newCount}");

            //SaveChanges-ის შემდეგ ახალ ჩანაწერებს იდენტიფიკატორები აქვს მინიჭებული და კავშირებში გამოყენებადია
            foreach ((string addedUrl, UrlModel urlModel) in addedUrls)
            {
                _urlIds[addedUrl] = urlModel.UrlId;
            }
        }

        if (fromUrl is not null)
        {
            PersistUrlGraphNodes(fromUrl, urlList);
        }

        return newCount;
    }

    //რომელ გვერდზე რომელი მისამართი მოიძებნა — გრაფის კავშირების შენახვა. მხოლოდ Urls-ში არსებულ
    //მისამართებს შორის: ფილტრში ჩაჭრილი ან ზღვარგადაცილებული მისამართები ბაზაში არ არის და კავშირიც არ ჩაიწერება
    private void PersistUrlGraphNodes(string fromUrl, IReadOnlyCollection<string> urlList)
    {
        if (!TryGetUrlId(fromUrl.TrimEnd('/'), out int fromUrlId))
        {
            return;
        }

        var newPairsCount = 0;
        foreach (string url in urlList.Select(s => s.TrimEnd('/')).Where(IsLikeStartPoint))
        {
            //ნაპოვნი მისამართები PersistNewUrls-მა უკვე ჩაწერა ქეშში და ბაზაში ხელახლა ძებნა საჭირო აღარ არის;
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

    //მისამართის UrlId ჯერ ამ გაშვების ქეშში იძებნება, შემდეგ ბაზაში ხეშ-კოდით — წყარო გვერდი (fromUrl)
    //წინა გაშვებაში შენახული ჩანაწერიც შეიძლება იყოს, რომელიც ქეშში ჯერ არ მოხვედრილა
    private bool TryGetUrlId(string url, out int urlId)
    {
        if (_urlIds.TryGetValue(url, out urlId))
        {
            return true;
        }

        if (!_repository.GetUrlIdsByUrlHashCode(url.GetDeterministicHashCode()).TryGetValue(url, out urlId))
        {
            return false;
        }

        _urlIds[url] = urlId;
        return true;
    }

    private bool IsLikeStartPoint(string url)
    {
        return _startPointPatterns.Exists(p =>
            url.Equals(p.Exact, StringComparison.Ordinal) || url.StartsWith(p.Prefix, StringComparison.Ordinal));
    }
}
