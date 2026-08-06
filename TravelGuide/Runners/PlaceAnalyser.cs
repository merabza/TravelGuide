using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SystemTools.SystemToolsShared;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Runners;

//ფაზა 2: დასამუშავებელი გვერდები ბრაუზერის გარეშე, პირდაპირ HTTP-ით მოიქაჩება და მონაცემები HTML-იდან ამოიღება
// ReSharper disable once ConvertToPrimaryConstructor
public sealed class PlaceAnalyser
{
    private readonly HttpClient _httpClient;
    private readonly PlaceLinksSynchronizer _placeLinksSynchronizer;
    private readonly bool _reProcessAnalysed;
    private readonly ITravelGuideRepository _repository;

    public PlaceAnalyser(HttpClient httpClient, ITravelGuideRepository repository, bool reProcessAnalysed)
    {
        _httpClient = httpClient;
        _repository = repository;
        _reProcessAnalysed = reProcessAnalysed;
        _placeLinksSynchronizer = new PlaceLinksSynchronizer(repository);
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _placeLinksSynchronizer.EnsureMonths();

        List<PlaceModel> places = _repository.GetPlacesForAnalysis(_reProcessAnalysed);
        var counter = 0;
        foreach (PlaceModel place in places)
        {
            counter++;
            Console.WriteLine($"({counter}/{places.Count}) {place.Url}");
            if (!await TryAnalysePlaceAsync(place, cancellationToken).ConfigureAwait(false))
            {
                StShared.WriteErrorLine($"Failed to analyse {place.Url}", true, null, false);
            }
        }
    }

    private async Task<bool> TryAnalysePlaceAsync(PlaceModel place, CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response =
                await _httpClient.GetAsync(new Uri(place.Url), cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                //წარუმატებელი პასუხისას ჩანაწერი New რჩება, რომ მომდევნო გაშვებამ თავიდან სცადოს
                StShared.WriteErrorLine($"Request failed with status {(int)response.StatusCode} for {place.Url}",
                    true, null, false);
                return false;
            }

            string html = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            PlaceExtractResult extract = PlaceDataExtractor.Extract(html);

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
