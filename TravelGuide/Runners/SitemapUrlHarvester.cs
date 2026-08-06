using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using SystemTools.SystemToolsShared;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Runners;

//საიტის sitemap-იდან ყველა გვერდის მისამართის შეგროვება — Selenium-ით სიის გვერდის ჩამოსქროლვის ალტერნატივა.
//sitemap-ში ღირსშესანიშნაობებთან ერთად ქალაქების/რეგიონების გვერდებიც ხვდება — მათ ანალიზის ფაზა
//JSON-LD-ის ტიპით არჩევს და NotAttraction-ად ნიშნავს.
// ReSharper disable once ConvertToPrimaryConstructor
public sealed class SitemapUrlHarvester
{
    //sitemap.xml ინდექსია და ენების მიხედვით ცალ-ცალკე sitemap ფაილებზე მიუთითებს — გვჭირდება ქართულენოვანი
    private const string GeorgianSitemapMarker = "_ka";

    private readonly HttpClient _httpClient;
    private readonly ITravelGuideRepository _repository;
    private readonly string _startPoint;

    public SitemapUrlHarvester(HttpClient httpClient, string startPoint, ITravelGuideRepository repository)
    {
        _httpClient = httpClient;
        _startPoint = startPoint;
        _repository = repository;
    }

    public async Task<bool> RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            //sitemap-ის მისამართი საწყისი წერტილის ჰოსტიდან იგება
            var sitemapIndexUri = new Uri(new Uri(_startPoint), "/sitemap.xml");
            Console.WriteLine($"Loading sitemap index {sitemapIndexUri}");
            XDocument sitemapIndex = await LoadXmlAsync(sitemapIndexUri, cancellationToken).ConfigureAwait(false);

            //ინდექსიდან ქართული sitemap-ის მისამართი ამოირჩევა; თუ პასუხი პირდაპირ urlset აღმოჩნდა, ისვე გამოიყენება
            XDocument urlSet = sitemapIndex;
            if (sitemapIndex.Root?.Name.LocalName == "sitemapindex")
            {
                string? georgianLoc = ExtractLocValues(sitemapIndex)
                    .Find(f => f.Contains(GeorgianSitemapMarker, StringComparison.Ordinal));
                if (georgianLoc is null)
                {
                    StShared.WriteErrorLine("Georgian sitemap was not found in sitemap index", true);
                    return false;
                }

                Console.WriteLine($"Loading sitemap {georgianLoc}");
                urlSet = await LoadXmlAsync(new Uri(georgianLoc), cancellationToken).ConfigureAwait(false);
            }

            List<string> urlList = ExtractLocValues(urlSet);
            HarvestedUrlPersister.PersistNewUrls(_repository, urlList);
            return true;
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true, null, false);
            return false;
        }
    }

    private async Task<XDocument> LoadXmlAsync(Uri uri, CancellationToken cancellationToken)
    {
        string xml = await _httpClient.GetStringAsync(uri, cancellationToken).ConfigureAwait(false);
        return XDocument.Parse(xml);
    }

    private static List<string> ExtractLocValues(XDocument document)
    {
        //ელემენტები namespace-ის გარეშე, ლოკალური სახელით მოიძებნება, რომ სქემის მისამართის ცვლილებამ არ გატეხოს
        return
        [
            .. document.Descendants().Where(w => w.Name.LocalName == "loc").Select(s => s.Value.Trim())
                .Where(w => w.Length > 0)
        ];
    }
}
