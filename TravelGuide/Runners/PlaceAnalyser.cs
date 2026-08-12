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
    private readonly bool _retryDownloadErrors;
    private readonly HarvestedUrlPersister _urlPersister;

    public PlaceAnalyser(HttpClient httpClient, ITravelGuideRepository repository, HarvestedUrlPersister urlPersister,
        bool reProcessAnalysed, bool retryDownloadErrors)
    {
        _httpClient = httpClient;
        _repository = repository;
        _urlPersister = urlPersister;
        _reProcessAnalysed = reProcessAnalysed;
        _retryDownloadErrors = retryDownloadErrors;
        _placeLinksSynchronizer = new PlaceLinksSynchronizer(repository);
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _placeLinksSynchronizer.EnsureMonths();

        //ერთ გაშვებაზე თითო გვერდი მხოლოდ ერთხელ მუშავდება: წარუმატებელი შეცდომის სტატუსით ინიშნება და
        //ამ ციკლში აღარ ბრუნდება, რომ ციკლი აუცილებლად დასრულდეს
        var attemptedIds = new HashSet<int>();

        //უკვე გაანალიზებულების ხელახლა დამუშავება მხოლოდ პირველ ულუფას ეხება —
        //მომდევნო ულუფები დამუშავებისას აღმოჩენილი ახალი მისამართებია
        bool includeAnalysed = _reProcessAnalysed;

        while (!cancellationToken.IsCancellationRequested)
        {
            List<PlaceModel> places =
            [
                .. _repository.GetPlacesForAnalysis(includeAnalysed, _retryDownloadErrors)
                    .Where(w => !attemptedIds.Contains(w.PlaceId))
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
                    //შეჩერების მოთხოვნით გამოწვეული ჩავარდნა შეცდომა არ არის — ჩანაწერი უცვლელი რჩება
                    //და მომდევნო გაშვება ჩვეულებრივ დაამუშავებს
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    StShared.WriteErrorLine($"Failed to analyse {place.Url}", true, null, false);

                    //ჩავარდნილი გვერდი შეცდომის სტატუსით ინიშნება — ხელახლა ცდა მომდევნო გაშვებისას
                    //მომხმარებლის დასტურზეა დამოკიდებული
                    place.State = EState.DownloadError;
                    _repository.SaveChanges();
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
                //წარუმატებელი პასუხისას false ბრუნდება და გამომძახებელი ჩანაწერს შეცდომის სტატუსით მონიშნავს
                StShared.WriteErrorLine($"Request failed with status {(int)response.StatusCode} for {place.Url}",
                    true, null, false);
                return false;
            }

            //ზოგი მისამართი საიტზე მუდმივი გადამისამართებით სხვა (კანონიკურ) მისამართზე გადადის და HttpClient
            //მას ჩუმად მიჰყვება — ორივე მისამართი ერთსა და იმავე გვერდს ცალ-ცალკე ჩანაწერად ინახავდა.
            //საბოლოო მისამართი place-ის შეცვლამდე რიგში ემატება (კანონიკურ გვერდს ამავე გაშვების ციკლი
            //დაამუშავებს), place.Url წყარო გვერდად გადაეცემა, რომ დუბლიკატი→კანონიკური კავშირი
            //UrlGraphNodes-შიც ჩაიწეროს, თავად ჩანაწერი კი დუბლიკატად ინიშნება და ანალიზში აღარ ბრუნდება.
            //მხოლოდ ბოლო „/"-ით განსხვავება გადამისამართებად არ ითვლება — ბაზაში მისამართები უიმისოდ ინახება.
            //შიგთავსი განზრახ არ იპარსება: ბმულები კანონიკური გვერდისაა და ძველ მისამართს მიეწერებოდა
            string finalUrl = (response.RequestMessage?.RequestUri ?? pageUri).AbsoluteUri.TrimEnd('/');
            if (!finalUrl.Equals(pageUri.AbsoluteUri.TrimEnd('/'), StringComparison.Ordinal))
            {
                _urlPersister.PersistNewUrls([finalUrl], place.Url);
                place.State = EState.Duplicate;
                _repository.SaveChanges();
                Console.WriteLine($"Duplicate page (redirected to {finalUrl}): {place.Url}");
                return true;
            }

            string html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using IHtmlDocument document =
                await new HtmlParser().ParseDocumentAsync(html, cancellationToken).ConfigureAwait(false);

            //გვერდზე ნაპოვნი ბმულები place-ის შეცვლამდე ინახება — ღირსშესანიშნაობის გარდა სხვა გვერდებიც
            //(რეგიონები, სიის გვერდები) ახალი მისამართების წყაროა; place.Url წყარო გვერდად გადაეცემა,
            //რომ ნაპოვნი კავშირები UrlGraphNodes-შიც ჩაიწეროს
            _urlPersister.PersistNewUrls(PageLinkExtractor.ExtractLinks(document, pageUri), place.Url);

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
