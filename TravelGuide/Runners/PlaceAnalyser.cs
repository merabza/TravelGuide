using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using SystemTools.SystemToolsShared;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Runners;

//ფაზა 2: ჩამოსატვირთი გვერდები ბაზიდან ულუფებად იტვირთება და სათითაოდ, ბრაუზერის გარეშე, პირდაპირ HTTP-ით
//მოიქაჩება; მონაცემები HTML-იდან ამოიღება, გვერდზე ნაპოვნი ახალი ბმულები კი ისევ ბაზაში ემატება.
//ციკლი გრძელდება, სანამ დასამუშავებელი აღარაფერი დარჩება (CrawlerService-ის BatchPartRunner-ის ანალოგია) —
//მდგომარეობა ბაზაშია და შეწყვეტილი პროცესი მომდევნო გაშვებისას გრძელდება
// ReSharper disable once ConvertToPrimaryConstructor
public sealed class PlaceAnalyser
{
    private readonly HttpClient _httpClient;
    private readonly PlaceLinksSynchronizer _placeLinksSynchronizer;
    private readonly bool _reProcessAnalysed;
    private readonly ITravelGuideRepository _repository;
    private readonly HarvestedUrlPersister _urlPersister;

    public PlaceAnalyser(HttpClient httpClient, ITravelGuideRepository repository, HarvestedUrlPersister urlPersister,
        bool reProcessAnalysed)
    {
        _httpClient = httpClient;
        _repository = repository;
        _urlPersister = urlPersister;
        _reProcessAnalysed = reProcessAnalysed;
        _placeLinksSynchronizer = new PlaceLinksSynchronizer(repository);
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _placeLinksSynchronizer.EnsureMonths();

        //ერთ გაშვებაზე თითო გვერდი მხოლოდ ერთხელ მუშავდება: წარუმატებელი New-დ რჩება (მომდევნო გაშვება სცდის),
        //მაგრამ ამ ციკლში აღარ ბრუნდება, რომ ციკლი აუცილებლად დასრულდეს
        var attemptedIds = new HashSet<int>();

        //უკვე გაანალიზებულების ხელახლა დამუშავება მხოლოდ პირველ ულუფას ეხება —
        //მომდევნო ულუფები დამუშავებისას აღმოჩენილი ახალი მისამართებია
        bool includeAnalysed = _reProcessAnalysed;

        while (!cancellationToken.IsCancellationRequested)
        {
            List<PlaceModel> places =
            [
                .. _repository.GetPlacesForAnalysis(includeAnalysed).Where(w => !attemptedIds.Contains(w.PlaceId))
            ];
            includeAnalysed = false;

            if (places.Count == 0)
            {
                break;
            }

            Console.WriteLine($"Loaded {places.Count} places for analysis");

            var counter = 0;
            foreach (PlaceModel place in places)
            {
                //თუ მოთხოვნილია პროცესის შეჩერება, გამოვიდეთ მეთოდიდან — დარჩენილს მომდევნო გაშვება დაამუშავებს
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                counter++;
                attemptedIds.Add(place.PlaceId);
                Console.WriteLine($"({counter}/{places.Count}) {place.Url}");
                if (!await TryAnalysePlaceAsync(place, cancellationToken).ConfigureAwait(false))
                {
                    StShared.WriteErrorLine($"Failed to analyse {place.Url}", true, null, false);
                }
            }
        }
    }

    private async Task<bool> TryAnalysePlaceAsync(PlaceModel place, CancellationToken cancellationToken)
    {
        try
        {
            var pageUri = new Uri(place.Url);
            using HttpResponseMessage response =
                await _httpClient.GetAsync(pageUri, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                //წარუმატებელი პასუხისას ჩანაწერი New რჩება, რომ მომდევნო გაშვებამ თავიდან სცადოს
                StShared.WriteErrorLine($"Request failed with status {(int)response.StatusCode} for {place.Url}",
                    true, null, false);
                return false;
            }

            string html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using IHtmlDocument document =
                await new HtmlParser().ParseDocumentAsync(html, cancellationToken).ConfigureAwait(false);

            //გვერდზე ნაპოვნი ბმულები place-ის შეცვლამდე ინახება — ღირსშესანიშნაობის გარდა სხვა გვერდებიც
            //(რეგიონები, სიის გვერდები) ახალი მისამართების წყაროა
            _urlPersister.PersistNewUrls(PageLinkExtractor.ExtractLinks(document, pageUri));

            PlaceExtractResult extract = PlaceDataExtractor.Extract(document);

            //ქალაქების/რეგიონების გვერდები (sitemap-იდან მოსული) ღირსშესანიშნაობებს არ წარმოადგენს —
            //ისინი ერთხელ ინიშნება და ანალიზში აღარ ბრუნდება
            if (!extract.IsTouristAttraction)
            {
                place.State = EState.NotAttraction;
                _repository.SaveChanges();
                Console.WriteLine($"Not a tourist attraction page: {place.Url}");
                return true;
            }

            if (string.IsNullOrWhiteSpace(extract.Name))
            {
                return false;
            }

            //ენთითი მხოლოდ სრული წარმატების შემდეგ იცვლება, რომ ნახევრად შევსებული ველები ბაზაში არ მოხვდეს;
            //SyncPlaceLinks-იც ჯერ საჭირო ჩანაწერებს ეძებს/ქმნის და place-ს მხოლოდ ბოლოს ცვლის
            _placeLinksSynchronizer.SyncPlaceLinks(extract, place);
            PlaceDataExtractor.Apply(extract, place);
            place.State = EState.Analysed;
            _repository.SaveChanges();
            return true;
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true, null, false);
            return false;
        }
    }
}
