using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenQA.Selenium;
using TravelGuideDbModels;
using TravelGuideDbPersistence.Configurations;

namespace TravelGuide.Runners;

public static partial class PlaceDataExtractor
{
    //მთელი ინფორმაცია ერთი ExecuteScript გამოძახებით გროვდება, რომ ImplicitWait-მა ელემენტების ძებნა არ დააყოვნოს.
    //JSON-LD პირველადი წყაროა (კოორდინატები სრული სიზუსტით), ხილული DOM — დანარჩენი ველებისთვის და fallback-ად.
    //JS აბრუნებს JSON string-ს PascalCase გასაღებებით, რომ PlaceExtractResult-ზე ოფშენების გარეშე დესერიალიზდეს.
    private const string ExtractionScript = """
        const result = { Name: null, Latitude: null, Longitude: null, Region: null, Municipality: null,
          Categories: [], Tags: [], BestSeason: null, Distances: [], Description: null };

        for (const s of document.querySelectorAll('script[type="application/ld+json"]')) {
          let data;
          try { data = JSON.parse(s.textContent); } catch (e) { continue; }
          const graph = Array.isArray(data) ? data : (data['@graph'] || [data]);
          for (const node of graph) {
            const types = Array.isArray(node['@type']) ? node['@type'] : [node['@type']];
            if (types.includes('TouristAttraction')) {
              if (node.name) { result.Name = node.name; }
              if (node.geo) {
                if (typeof node.geo.latitude === 'number') { result.Latitude = node.geo.latitude; }
                if (typeof node.geo.longitude === 'number') { result.Longitude = node.geo.longitude; }
              }
            }
            if (types.includes('BreadcrumbList') && Array.isArray(node.itemListElement)) {
              const mids = node.itemListElement.slice(2, -1).map(i => i.name).filter(n => n);
              if (mids.length > 0) { result.Region = mids[0]; }
              if (mids.length > 1) { result.Municipality = mids[1]; }
            }
          }
        }

        if (!result.Name) {
          const h1 = document.querySelector('h1');
          if (h1) { result.Name = h1.innerText.trim(); }
        }

        result.Categories = Array.from(document.querySelectorAll('.destination-header .categories a'))
          .map(a => a.innerText.trim()).filter(t => t);
        result.Tags = Array.from(document.querySelectorAll('section.tags a'))
          .map(a => a.innerText.trim()).filter(t => t);

        const season = document.querySelector('.best-time-to-visit strong');
        if (season) { result.BestSeason = season.innerText.trim(); }

        if (!result.Region) {
          const loc = document.querySelector('.destination-header .location a');
          if (loc) {
            const clone = loc.cloneNode(true);
            clone.querySelectorAll('span').forEach(x => x.remove());
            const parts = clone.textContent.split(',').map(p => p.trim()).filter(p => p);
            if (parts.length > 0) { result.Region = parts[0]; }
            if (parts.length > 1) { result.Municipality = parts[1]; }
          }
        }

        const items = Array.from(document.querySelectorAll('.technical-details .item'));
        if (result.Latitude === null || result.Longitude === null) {
          const coordItem = items.find(i => i.querySelector('span.icon-location'));
          const content = coordItem ? coordItem.querySelector('div.content') : null;
          if (content) {
            const nums = content.textContent.split(',').map(p => parseFloat(p.trim())).filter(n => !isNaN(n));
            if (nums.length === 2) { result.Latitude = nums[0]; result.Longitude = nums[1]; }
          }
        }

        const distItem = items.find(i => i.querySelector('span.icon-map-signs'));
        if (distItem) {
          result.Distances = Array.from(distItem.querySelectorAll('ul.sub-content li'))
            .map(li => li.textContent.replace(/\u00A0/g, ' ').replace(/\s+/g, ' ').trim()).filter(t => t);
        }

        const wrap = document.querySelector('.collapsed-text');
        if (wrap) { wrap.style.maxHeight = 'none'; wrap.style.height = 'auto'; wrap.style.overflow = 'visible'; }
        const ed = document.querySelector('.collapsed-text .editor-text') || document.querySelector('.editor-text');
        if (ed) { result.Description = ed.innerText.trim(); }

        return JSON.stringify(result);
        """;

    public static PlaceExtractResult? TryExtract(IWebDriver driver)
    {
        var json = ((IJavaScriptExecutor)driver).ExecuteScript(ExtractionScript) as string;
        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<PlaceExtractResult>(json);
    }

    public static void Apply(PlaceExtractResult extract, PlaceModel place)
    {
        place.Name = Truncate(extract.Name, PlaceModelConfiguration.NameLength);
        place.Latitude = extract.Latitude;
        place.Longitude = extract.Longitude;
        place.Region = Truncate(extract.Region, PlaceModelConfiguration.RegionLength);
        place.Municipality = Truncate(extract.Municipality, PlaceModelConfiguration.MunicipalityLength);
        place.Distances = extract.Distances;
        place.DistanceFromTbilisiKm = ParseDistanceFromTbilisiKm(extract.Distances);
        place.Description = extract.Description;
    }

    private static string? Truncate(string? value, int maxLength)
    {
        return value is null || value.Length <= maxLength ? value : value[..maxLength];
    }

    private static int? ParseDistanceFromTbilisiKm(IEnumerable<string> distances)
    {
        //აეროპორტების ჩანაწერები „აეროპორტიდან"-ზე მთავრდება, ამიტომ „თბილისიდან"-ზე დამთავრება მათ ვერ დაემთხვევა
        string? tbilisiItem = distances.FirstOrDefault(d => d.EndsWith("თბილისიდან", StringComparison.Ordinal));
        if (tbilisiItem is null)
        {
            return null;
        }

        Match match = LeadingNumberRegex().Match(tbilisiItem);
        return match.Success ? int.Parse(match.Value, CultureInfo.InvariantCulture) : null;
    }

    [GeneratedRegex(@"^\d+")]
    private static partial Regex LeadingNumberRegex();
}
