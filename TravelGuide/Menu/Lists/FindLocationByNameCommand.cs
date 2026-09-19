using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.LibDataInput;
using AppCliTools.LibMenuInput;
using SystemTools.SystemToolsShared;
using TravelGuide.Runners;
using TravelGuideCore.Domain.LocationModels;
using TravelGuideCore.Domain.PlaceModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Lists;

//ადგილის ლოკაციის დადგენა დასახელებით — ადგილის ჩანაწერის მენიუს პუნქტი ველების რედაქტორების შემდეგ; ახალი ადგილის
//შექმნისას იგივე ბრძანება შენახვისთანავე, საძიებო ტექსტის კითხვის გარეშე ეშვება (PlaceCruder.AddRecordWithKey).
//საძიებო ტექსტი (ნაგულისხმევად ადგილის დასახელება) OpenStreetMap-ის საჯარო გეოკოდერს (Nominatim) ეძებნება
//საქართველოს ფარგლებში — ჯერ მთლიანად, ვერაფრის პოვნისას ნაწილებით. სერვისს არ სჭირდება ტექსტი, რომელიც მძიმით
//გამოყოფილი ორი რიცხვით იწყება (ისინი თავად კოორდინატებია) ან Google-ის Plus Code-ს შეიცავს („6H9G+H76 თბილისი“ —
//კოდი იშიფრება, მოკლე კოდის საყრდენად დანარჩენი ტექსტი იძებნება). ნაპოვნი კანდიდატებიდან მომხმარებელი ერთს ირჩევს
//და მისი კოორდინატები ადგილს ლოკაციად ებმება ისევე, როგორც ლოკაციების რედაქტორში (PlaceLocationCruder) ხელით
//შეყვანილი წყვილი: Locations საზიარო ჩანაწერია და არსებული მეორდება ან ახალი იქმნება. ძირითადად ხელით შეყვანილი
//ადგილისთვისაა, რომელსაც საიტიდან კოორდინატები არ მოჰყვება; ადგილის არსებულ ლოკაციებს არ ცვლის — ახალს უმატებს.
//სერვისის მისამართიდან რეგიონი და მუნიციპალიტეტიც იკითხება და ადგილის მიმდინარე მნიშვნელობებს ედრება —
//ცარიელი ცალსახა შესატყვისით ივსება, შეუსაბამობისას ცნობარების შესატყვისი ჩანაწერებით განახლება სთავაზობს
//(OfferLookupUpdates)
public sealed partial class FindLocationByNameCommand : CliMenuCommand
{
    //ერთ პასუხში კანდიდატების მაქსიმალური რაოდენობა — მენიუს ციფრული გასაღებები (0-9) ჰყოფნის
    private const int MaxCandidatesCount = 10;

    //ქართული ხმოვნები — ცნობარის სახელის ფუძის გამოსაყოფად (LookupNameMatches)
    private const string GeorgianVowels = "აეიოუ";

    //Nominatim-ის საჯარო სერვისს ზედიზედ მოთხოვნები წამში ერთზე ხშირად არ უნდა გაეგზავნოს (ნაწილებით ძებნისას)
    private static readonly TimeSpan RequestDelay = TimeSpan.FromSeconds(1);

    //ტექსტის ნაწილების გამყოფები (EnumerateSearchTexts): დეფისი არა — ის სახელწოდების ნაწილია („მცხეთა-მთიანეთი“)
    private static readonly char[] SegmentSeparators = [',', ';', '(', ')', '/'];
    private static readonly char[] WordSeparators = [' ', '\t', ',', ';', '(', ')', '/'];

    //ტექსტიდან Plus Code-ის ამოღების შემდეგ დანარჩენს ჰარები და გამყოფები ეჭრება
    private static readonly char[] RemainderTrimChars = [' ', ',', ';'];

    private readonly bool _askSearchText;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly PlaceModel _place;
    private readonly string _placeCaption;
    private readonly PlaceCruder _placeCruder;
    private readonly ITravelGuideRepository _travelGuideRepository;

    //place ადგილის პორციის ასლია, რომელსაც ველების რედაქტორები ცვლიან — დასახელება მასში მიმდინარეა და რეგიონისა
    //და მუნიციპალიტეტის განახლებაც მასზე იწერება და placeCruder-ის ჩვეულებრივი გზით ინახება; placeCaption
    //ჩანაწერის მენიუს პუნქტის სახელია (წარწერა, რედაქტორის გასაღები) შეტყობინებებისა და შენახვისთვის;
    //askSearchText=false-ით (ახალი ადგილის შექმნისას) საძიებო ტექსტი არ იკითხება და დასახელება პირდაპირ იძებნება
    public FindLocationByNameCommand(PlaceCruder placeCruder, ITravelGuideRepository travelGuideRepository,
        IHttpClientFactory httpClientFactory, PlaceModel place, string placeCaption, bool askSearchText = true) : base(
        "Find Location by Name", EMenuAction.Reload)
    {
        _placeCruder = placeCruder;
        _travelGuideRepository = travelGuideRepository;
        _httpClientFactory = httpClientFactory;
        _place = place;
        _placeCaption = placeCaption;
        _askSearchText = askSearchText;
    }

    protected override async ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        //საძიებო ტექსტი ნაგულისხმევად ადგილის დასახელებაა — Enter მას უცვლელად იყენებს; დაზუსტება შეიძლება
        //(მაგალითად სოფლის მიწერა), როცა მარტო დასახელებით სასურველი არ მოიძებნება. უსახელო ადგილს ტექსტი
        //თავიდან უნდა ჩაეწეროს. შექმნისას (askSearchText=false) სახელიანი ადგილის დასახელება კითხვის გარეშე იძებნება.
        //Escape-ის გამონაკლისს საბაზო Run იჭერს
        string? defaultSearchText = string.IsNullOrWhiteSpace(_place.Name) ? null : _place.Name;
        string? searchText = _askSearchText || defaultSearchText is null
            ? Inputer.InputText("Search Text", defaultSearchText)?.Trim()
            : defaultSearchText.Trim();
        if (string.IsNullOrEmpty(searchText))
        {
            StShared.WriteErrorLine("Search Text is empty", true);
            return false;
        }

        List<LocationCandidate>? candidates = await GetCandidates(searchText, cancellationToken);
        if (candidates is null)
        {
            return false;
        }

        //კანდიდატების სია: პუნქტის სახელი კოორდინატებია (ლოკაციების რედაქტორის გასაღების ფორმატით), სტატუსში —
        //ობიექტის ტიპი და სრული მისამართი (ტექსტიდან წაკითხული კოორდინატებისა და Plus Code-ისთვის — ერთადერთი
        //პუნქტი ამის აღნიშვნით); Enter პირველს (სერვისის აზრით ყველაზე შესაფერისს) ირჩევს, Escape-ის გამონაკლისს
        //საბაზო Run იჭერს და არაფერი ინახება
        var listSet = new CliMenuSet();
        foreach (LocationCandidate candidate in candidates)
        {
            listSet.AddMenuItem(new MenuCommandWithStatusCliMenuCommand(candidate.Key, candidate.Description));
        }

        string? selectedKey = MenuInputer.InputFromMenuList("Location", listSet, candidates[0].Key);
        LocationCandidate? selected = candidates.Find(f => f.Key == selectedKey);
        if (selected is null)
        {
            StShared.WriteErrorLine("Location is not selected", true);
            return false;
        }

        //ლოკაცია არსებულთაგან მოიძებნება ან იქმნება; ამ ადგილზე უკვე მიბმულისთვის შეცდომა იწერება — ახლადშექმნილს
        //გასაღები ჯერ არ აქვს (LocationId=0) და მიბმული ვერ იქნება (როგორც PlaceLocationCruder-ში). რეგიონი და
        //მუნიციპალიტეტი მაშინაც მოწმდება — ბრძანების განმეორებით გაშვებას ამისთვისაც აქვს აზრი
        LocationModel location =
            _travelGuideRepository.GetOrCreateLocation(selected.Location.Latitude, selected.Location.Longitude);
        if (location.LocationId != 0 &&
            _travelGuideRepository.GetPlaceLocation(_place.PlaceId, location.LocationId) is not null)
        {
            StShared.WriteErrorLine($"Location {selected.Key} is already linked to {_placeCaption}", true);
        }
        else
        {
            _travelGuideRepository.AddPlaceLocation(_place.PlaceId, location);

            _travelGuideRepository.SaveChanges();
            Console.WriteLine($"Location {selected.Key} linked to {_placeCaption}");
        }

        await OfferLookupUpdates(selected, cancellationToken);
        return true;
    }

    //სერვისის მისამართიდან წაკითხული რეგიონი და მუნიციპალიტეტი ადგილის მიმდინარე მნიშვნელობებს ედრება და
    //შეუსაბამობისას განახლება სთავაზობს; ტექსტიდან წაკითხულ კოორდინატებსა და სრულ Plus Code-ს მისამართი არ ახლავს
    //და არაფერი მოწმდება. არჩეული მნიშვნელობები ადგილის ასლზე იწერება და რედაქტორის ჩვეულებრივი გზით ინახება
    //(UpdateRecordWithKey — ბმულ ჩანაწერს ასლიდან გადააქვს), ამიტომ ჩანაწერის მენიუს ველებშიც მაშინვე ჩანს
    private async ValueTask OfferLookupUpdates(LocationCandidate candidate, CancellationToken cancellationToken)
    {
        var changed = false;
        if (candidate.Region is { } region && OfferLookupUpdate("Region", region, _place.RegionId,
                _travelGuideRepository.GetRegionsList().ToDictionary(k => k.RegionId, v => v.Name)) is { } regionId)
        {
            _place.RegionId = regionId;
            changed = true;
        }

        if (candidate.Municipality is { } municipality && OfferLookupUpdate("Municipality", municipality,
                _place.MunicipalityId,
                _travelGuideRepository.GetMunicipalitiesList().ToDictionary(k => k.MunicipalityId, v => v.Name)) is
            { } municipalityId)
        {
            _place.MunicipalityId = municipalityId;
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        await _placeCruder.UpdateRecordWithKey(_placeCaption, _place, cancellationToken);
        Console.WriteLine($"{_placeCaption} updated");
    }

    //ერთი ცნობარის შემოწმება: foundName სერვისის მისამართიდან წაკითხული სახელია, currentId — ადგილის მიმდინარე
    //მნიშვნელობა, lookup — ცნობარი (იდენტიფიკატორი → სახელი). მიმდინარე მნიშვნელობა სერვისისას რომ შეესაბამებოდეს
    //(LookupNameMatches), არაფერი იკითხება; შესატყვისი ჩანაწერი რომ არ იყოს, მხოლოდ შეტყობინება იწერება — ცნობარში
    //ახალი ჩანაწერი აქედან არ იქმნება (სერვისის სახელწოდება ცნობარის ფორმას არ ემთხვევა); ცარიელი მნიშვნელობა
    //ერთადერთი შესატყვისით კითხვის გარეშე ივსება (ასე ივსება ახალი ადგილის რეგიონი და მუნიციპალიტეტი). სხვა
    //შემთხვევაში შესატყვისებიდან ასარჩევია (Enter პირველს იღებს, „-“ უცვლელად ტოვებს); აბრუნებს არჩეულ
    //იდენტიფიკატორს ან null
    private static int? OfferLookupUpdate(string fieldName, string foundName, int? currentId,
        Dictionary<int, string> lookup)
    {
        List<KeyValuePair<int, string>> matches = [.. lookup.Where(w => LookupNameMatches(w.Value, foundName))];
        if (matches.Exists(e => e.Key == currentId))
        {
            return null;
        }

        string currentName = currentId is { } id && lookup.TryGetValue(id, out string? name) ? name : "(None)";
        if (matches.Count == 0)
        {
            Console.WriteLine(
                $"{fieldName} {foundName} (current {currentName}) matches no entry in the {fieldName} list");
            return null;
        }

        if (currentId is null && matches.Count == 1)
        {
            Console.WriteLine($"{fieldName} set to {matches[0].Value} ({foundName})");
            return matches[0].Key;
        }

        Console.WriteLine($"{fieldName} found: {foundName} (current {currentName})");
        List<string> names = [.. matches.Select(s => s.Value)];
        var input = new SelectFromListInput($"new {fieldName}", names, names[0], true);
        if (!input.DoInput() || input.Text is null)
        {
            return null;
        }

        return matches[names.IndexOf(input.Text)].Key;
    }

    //ცნობარის სახელი სერვისის სახელს შეესაბამება, თუ მისი ფუძე (სახელი ბოლო ხმოვნის გარეშე — ნათესაობითში ბოლო
    //ხმოვანი იცვლება ან ს ემატება: „მცხეთა“ → „მცხეთის“, „სიღნაღი“ → „სიღნაღის“, „ხულო“ → „ხულოს“) სერვისის სახელს
    //სიტყვის დასაწყისში აქვს: თავში („კახეთის მხარე“ ← „კახეთი“) ან ჰარის/დეფისის შემდეგ („სამეგრელო-ზემო სვანეთის
    //მხარე“ ← „სვანეთი“, „რაჭა-ლეჩხუმისა და ქვემო სვანეთის მხარე“ ← „ლეჩხუმი“) — სიტყვის შუაში არა, რომ „ონი“
    //„ზესტაფონის“ არ დაემთხვეს. ქართულ ასოებს რეგისტრი არ აქვს და შედარება ორდინალურია
    private static bool LookupNameMatches(string lookupName, string foundName)
    {
        string stem = lookupName.Length > 1 && GeorgianVowels.Contains(lookupName[^1])
            ? lookupName[..^1]
            : lookupName;
        return foundName.StartsWith(stem, StringComparison.Ordinal) ||
               foundName.Contains(" " + stem, StringComparison.Ordinal) ||
               foundName.Contains("-" + stem, StringComparison.Ordinal);
    }

    //კანდიდატები საძიებო ტექსტიდან. ტექსტი მძიმით გამოყოფილი ორი რიცხვით რომ იწყებოდეს („41.83, 44.73 ...“),
    //ისინი თავად კოორდინატებია — სერვისს არ მიმართავს და რიცხვებს განედად და გრძედად კითხულობს. Plus Code-იანი
    //ტექსტი კოდით იშიფრება (GetPlusCodeCandidates). სხვა ტექსტი Nominatim-ით იძებნება — მთლიანად და, საჭიროებისას,
    //ნაწილებით (SearchLocationsWithParts). შეცდომისას შეტყობინება იწერება და null ბრუნდება
    private async Task<List<LocationCandidate>?> GetCandidates(string searchText,
        CancellationToken cancellationToken)
    {
        Match coordinatesMatch = LeadingCoordinatesRegex().Match(searchText);
        if (coordinatesMatch.Success)
        {
            return GetCoordinatesCandidates(coordinatesMatch);
        }

        Match plusCodeMatch = PlusCodeRegex().Match(searchText);
        if (plusCodeMatch.Success)
        {
            return await GetPlusCodeCandidates(plusCodeMatch, searchText, cancellationToken);
        }

        return await SearchLocationsWithParts(searchText, cancellationToken);
    }

    //ტექსტის სათავეში ჩაწერილი წყვილი; მსოფლიოს ზღვრებს გარეთ წყვილი შეცდომაა და არც სერვისს ეძებნება
    private static List<LocationCandidate>? GetCoordinatesCandidates(Match coordinatesMatch)
    {
        var location = new LocationItem
        {
            Latitude = double.Parse(coordinatesMatch.Groups["lat"].Value, CultureInfo.InvariantCulture),
            Longitude = double.Parse(coordinatesMatch.Groups["lon"].Value, CultureInfo.InvariantCulture)
        };
        if (!PlaceDataExtractor.IsValidCoordinatePair(location.Latitude, location.Longitude))
        {
            StShared.WriteErrorLine(
                $"Invalid coordinates {location.GetItemKey()}: latitude must be within [-90, 90] and longitude within [-180, 180]",
                true);
            return null;
        }

        return [new LocationCandidate(location, "coordinates from Search Text")];
    }

    //Plus Code-ის (Open Location Code) გაშიფვრა. სრული კოდი პირდაპირ იშიფრება; მოკლეს (Google Maps ასეთს აჩვენებს —
    //„6H9G+H76 თბილისი“) საყრდენი წერტილი სჭირდება: კოდის გარდა დარჩენილი ტექსტი (ჩვეულებრივ დასახლება) სერვისით
    //იძებნება და პირველი შედეგი საყრდენია — მისი მისამართიდან რეგიონი და მუნიციპალიტეტიც იკითხება. ტექსტში
    //დასახლების გარეშე მოკლე კოდი ვერ აღდგება (4 სიმბოლოს დაკლებისას საყრდენი 1°-ის ფარგლებში უნდა იყოს)
    private async Task<List<LocationCandidate>?> GetPlusCodeCandidates(Match plusCodeMatch, string searchText,
        CancellationToken cancellationToken)
    {
        string code = plusCodeMatch.Value.ToUpperInvariant();
        (double Latitude, double Longitude) center;
        string description;
        LocationCandidate? reference = null;
        if (PlusCode.IsFull(code))
        {
            center = PlusCode.Decode(code);
            description = $"Plus code {code}";
        }
        else
        {
            string remainder = searchText.Remove(plusCodeMatch.Index, plusCodeMatch.Length).Trim(RemainderTrimChars);
            if (remainder.Length == 0)
            {
                StShared.WriteErrorLine(
                    $"Plus code {code} is short and needs a locality name next to it (like \"{code} თბილისი\")", true);
                return null;
            }

            Console.WriteLine($"Plus code {code} is short — searching the reference Location for {remainder}");
            List<LocationCandidate>? references = await SearchLocationsWithParts(remainder, cancellationToken);
            if (references is null)
            {
                return null;
            }

            reference = references[0];
            center = PlusCode.RecoverNearest(code, reference.Location.Latitude, reference.Location.Longitude);
            description = $"Plus code {code} near {reference.Description}";
        }

        var location = new LocationItem { Latitude = center.Latitude, Longitude = center.Longitude };
        if (!PlaceDataExtractor.IsValidCoordinatePair(location.Latitude, location.Longitude))
        {
            StShared.WriteErrorLine($"Plus code {code} is invalid", true);
            return null;
        }

        return [new LocationCandidate(location, description, reference?.Region, reference?.Municipality)];
    }

    //ძებნა ჯერ მთელი ტექსტით, ვერაფრის პოვნისას — ნაწილებით (EnumerateSearchTexts), პირველივე შედეგიან ტექსტამდე;
    //ერთი და იგივე ტექსტი ორჯერ არ იძებნება და მოთხოვნებს შორის სერვისის წესებით წამიანი შესვენებაა. წარუმატებელი
    //მოთხოვნისას (ნაწილებით აღარ ცდის) და ვერაფრის პოვნისას შეცდომა იწერება და null ბრუნდება
    private async Task<List<LocationCandidate>?> SearchLocationsWithParts(string searchText,
        CancellationToken cancellationToken)
    {
        var triedTexts = new HashSet<string>(StringComparer.Ordinal);
        foreach (string text in EnumerateSearchTexts(searchText))
        {
            if (!triedTexts.Add(text))
            {
                continue;
            }

            if (triedTexts.Count > 1)
            {
                await Task.Delay(RequestDelay, cancellationToken);
            }

            Console.WriteLine($"Searching Location for {text}...");
            List<LocationCandidate>? candidates = await SearchLocations(text, cancellationToken);
            if (candidates is null)
            {
                StShared.WriteErrorLine("Location search request failed", true);
                return null;
            }

            if (candidates.Count > 0)
            {
                return candidates;
            }
        }

        StShared.WriteErrorLine($"No Locations found for {searchText}", true);
        return null;
    }

    //ძებნის ტექსტები თანმიმდევრობით: ჯერ მთელი ტექსტი, მერე ნაწილები — გამყოფებით (SegmentSeparators) გამოყოფილი
    //ნაწილები, სიტყვების თანმიმდევრობები ბოლოდან სიტყვების მოკლებით („A B C“ → „A B“ → „A“) და თავიდან („B C“ → „C“):
    //ჯერ ობიექტის სახელი ეძებნება, ბოლოს — დასახლება, რომელიც ჩვეულებრივ ბოლოშია
    private static IEnumerable<string> EnumerateSearchTexts(string searchText)
    {
        yield return searchText;

        string[] segments = searchText.Split(SegmentSeparators,
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length > 1)
        {
            foreach (string segment in segments)
            {
                yield return segment;
            }
        }

        string[] words = searchText.Split(WordSeparators,
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (int count = words.Length - 1; count >= 1; count--)
        {
            yield return string.Join(' ', words.Take(count));
        }

        for (int count = words.Length - 1; count >= 1; count--)
        {
            yield return string.Join(' ', words.Skip(words.Length - count));
        }
    }

    //საძიებო ტექსტის ძებნა Nominatim-ით საქართველოს ფარგლებში, ქართულენოვანი მისამართებით. წარუმატებლობისას
    //(ინტერნეტი, სერვისი, პასუხის ფორმატი) გამონაკლისის მაგივრად null ბრუნდება, როგორც
    //DistanceCounter.TryGetRoadRoute-ში. ერთნაირი კოორდინატების კანდიდატები ერთი და იგივე ლოკაციაა (Locations
    //წყვილით უნიკალურია) — პირველი რჩება, რომ სიაში პუნქტების სახელები არ გამეორდეს
    private async Task<List<LocationCandidate>?> SearchLocations(string searchText,
        CancellationToken cancellationToken)
    {
        try
        {
            var url = new Uri(string.Create(CultureInfo.InvariantCulture,
                $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(searchText)}&format=jsonv2&limit={MaxCandidatesCount}&countrycodes=ge&accept-language=ka&addressdetails=1"));

            // ReSharper disable once using
            using HttpClient httpClient = _httpClientFactory.CreateClient();
            // ReSharper disable once using
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            //საჯარო სერვისი გამოყენების წესებით მოთხოვნას აპლიკაციის ამომცნობ სათაურს ითხოვს
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("TravelGuideBot", "1.0"));

            //საჯარო სერვისმა შეიძლება არ უპასუხოს — ლოდინი 10 წამით იზღუდება
            // ReSharper disable once using
            using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(10));
            // ReSharper disable once using
            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationTokenSource.Token);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            // ReSharper disable once using
            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationTokenSource.Token);
            // ReSharper disable once using
            using JsonDocument jsonDocument =
                await JsonDocument.ParseAsync(stream, cancellationToken: cancellationTokenSource.Token);

            List<LocationCandidate> candidates = [];
            foreach (JsonElement element in jsonDocument.RootElement.EnumerateArray())
            {
                //კოორდინატები პასუხში ტექსტებია („41.8383412"); მსოფლიოს ზღვრებს გარეთ წყვილი ქროულერის შემოწმებით იფილტრება
                if (!TryGetCoordinate(element, "lat", out double latitude) ||
                    !TryGetCoordinate(element, "lon", out double longitude) ||
                    !PlaceDataExtractor.IsValidCoordinatePair(latitude, longitude))
                {
                    continue;
                }

                //ობიექტის ტიპი (მაგალითად monastery, village, peak) ერთნაირი სახელის კანდიდატებს განასხვავებს
                string? type = element.GetProperty("type").GetString();
                string? displayName = element.GetProperty("display_name").GetString();

                //მისამართის დეტალებში რეგიონი ადმინისტრაციული ერთეულია (state; თბილისს, როგორც ქალაქ-რეგიონს, მხოლოდ
                //city აქვს), მუნიციპალიტეტი — county (თვითმმართველ ქალაქს — city); სერვისი მხოლოდ არსებულ დონეებს აბრუნებს
                string? region = null;
                string? municipality = null;
                if (element.TryGetProperty("address", out JsonElement address))
                {
                    region = GetOptionalString(address, "state") ?? GetOptionalString(address, "city");
                    municipality = GetOptionalString(address, "county") ?? GetOptionalString(address, "city");
                }

                var candidate = new LocationCandidate(new LocationItem { Latitude = latitude, Longitude = longitude },
                    $"{type}: {displayName}", region, municipality);
                if (!candidates.Exists(e => e.Key == candidate.Key))
                {
                    candidates.Add(candidate);
                }
            }

            return candidates;
        }
        catch (Exception)
        {
            //ინტერნეტის, სერვისის ან პასუხის ფორმატის პრობლემისას null ბრუნდება და გამომძახებელი შეცდომას წერს
            return null;
        }
    }

    private static bool TryGetCoordinate(JsonElement element, string propertyName, out double value)
    {
        value = 0;
        return element.TryGetProperty(propertyName, out JsonElement property) &&
               double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static string? GetOptionalString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out JsonElement property) ? property.GetString() : null;
    }

    //ტექსტის სათავეში მძიმით გამოყოფილი ორი რიცხვი (ათწილადი წერტილით, შესაძლოა უარყოფითი), რომელთა შემდეგ ტექსტი
    //მთავრდება ან ჰარით/მძიმით გრძელდება — „41.8383412, 44.7335270 ჯვრის მონასტერი“
    [GeneratedRegex(@"^\s*(?<lat>-?\d+(?:\.\d+)?)\s*,\s*(?<lon>-?\d+(?:\.\d+)?)(?=[\s,]|$)")]
    private static partial Regex LeadingCoordinatesRegex();

    //Plus Code (Open Location Code) ტექსტში: გამყოფამდე 4, 6 ან 8 სიმბოლო (Google Maps მოკლე კოდს 4-ით აჩვენებს —
    //„6H9G+H76 თბილისი“; 2-იანი მოკლე ფორმა განზრახ არ ცნობდება, რომ „22+22“-ის მსგავსი ტექსტი კოდად არ ჩაითვალოს),
    //გამყოფის შემდეგ 2-7; ანბანში ხმოვნები არ არის და ჩვეულებრივ სიტყვას არ ემთხვევა; ხელით შეყვანილი მცირე ასოებიც გამოდგება
    [GeneratedRegex(@"(?:[23456789CFGHJMPQRVWX]{2}){2,4}\+[23456789CFGHJMPQRVWX]{2,7}",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PlusCodeRegex();

    //ნაპოვნი კანდიდატი: კოორდინატები, აღწერა (ობიექტის ტიპი და სრული მისამართი) და მისამართიდან წაკითხული რეგიონისა
    //და მუნიციპალიტეტის სახელები (ტექსტიდან წაკითხულ კოორდინატებს არ აქვს). გასაღები კოორდინატებია
    //ლოკაციების რედაქტორის ჩანაწერის გასაღების ფორმატით („განედი, გრძედი")
    private sealed record LocationCandidate(LocationItem Location, string Description, string? Region = null,
        string? Municipality = null)
    {
        public string Key => Location.GetItemKey();
    }
}
