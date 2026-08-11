using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using TravelGuideDbModels;
using TravelGuideDbPersistence.Configurations;

namespace TravelGuide.Runners;

public static partial class PlaceDataExtractor
{
    //აღწერის ტექსტში ამ ელემენტების დახურვისას ახალი ხაზი ჩაისმის, რომ აბზაცები ერთმანეთს არ შეეწებოს
    private static readonly HashSet<string> BlockTagNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "div", "li", "ul", "ol", "h1", "h2", "h3", "h4", "h5", "h6", "table", "tr", "blockquote"
    };

    //სერვერი სრულად დარენდერებულ HTML-ს აბრუნებს, ამიტომ ბრაუზერი საჭირო არ არის — საკმარისია მოქაჩული ტექსტის გაპარსვა.
    //დოკუმენტს გამომძახებელი პარსავს, რომ იგივე დოკუმენტი ბმულების ამოკრებასაც მოხმარდეს.
    //JSON-LD პირველადი წყაროა, ხილული DOM — დანარჩენი ველებისთვის და fallback-ად;
    //ლოკაციები ორივე წყაროდან ერთიანდება, რადგან სრულ სიას მხოლოდ ხილული DOM შეიცავს (იხ. BuildLocations).
    public static PlaceExtractResult Extract(IHtmlDocument document)
    {
        JsonLdData jsonLd = JsonLdData.Parse(document);

        string? name = jsonLd.Name ?? TrimmedTextOrNull(document.QuerySelector("h1"));

        string? region = jsonLd.Region;
        string? municipality = jsonLd.Municipality;
        if (region is null)
        {
            (region, municipality) = ParseLocationFallback(document);
        }

        return new PlaceExtractResult
        {
            IsTouristAttraction = jsonLd.IsTouristAttraction,
            Name = name,
            Locations = BuildLocations(jsonLd.Locations, ParseDomLocations(document)),
            Region = region,
            Municipality = municipality,
            Categories = SelectTexts(document, ".destination-header .categories a"),
            Tags = SelectTexts(document, "section.tags a"),
            BestSeason = TrimmedTextOrNull(document.QuerySelector(".best-time-to-visit strong")),
            Distances = ParseDistances(document),
            Description = ParseDescription(document)
        };
    }

    //რეგიონი, მუნიციპალიტეტი და ლოკაციები აქ არ ისმება — ისინი ცალკე ცხრილების ბმულებია და PlaceLinksSynchronizer აწესრიგებს
    public static void Apply(PlaceExtractResult extract, PlaceModel place)
    {
        place.Name = Truncate(extract.Name, PlaceModelConfiguration.NameLength);
        place.Description = extract.Description;
    }

    private static string? TrimmedTextOrNull(IElement? element)
    {
        string? text = element?.TextContent.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }

    private static List<string> SelectTexts(IHtmlDocument document, string selector)
    {
        return [.. document.QuerySelectorAll(selector).Select(s => s.TextContent.Trim()).Where(w => w.Length > 0)];
    }

    private static (string? Region, string? Municipality) ParseLocationFallback(IHtmlDocument document)
    {
        IElement? location = document.QuerySelector(".destination-header .location a");
        if (location is null)
        {
            return (null, null);
        }

        //ბმულს შიგნით დამხმარე წარწერიანი span-ები აქვს („(იხილეთ რუკაზე)"), ტექსტიდან ისინი იშლება
        foreach (IElement span in location.QuerySelectorAll("span").ToList())
        {
            span.Remove();
        }

        string[] parts = location.TextContent.Split(',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return (parts.Length > 0 ? parts[0] : null, parts.Length > 1 ? parts[1] : null);
    }

    //გეოგრაფიულად დასაშვები ზღვრები: ამათ გარეთ მოხვედრილი წყვილი (მაგ. UTM-ის მეტრული
    //მნიშვნელობები, როგორც 297267, 4768381) მცდარია და არ ინახება
    private const double MinValidLatitude = -90;
    private const double MaxValidLatitude = 90;
    private const double MinValidLongitude = -180;
    private const double MaxValidLongitude = 180;

    //ხილულ სიაში კოორდინატები 6 ნიშნამდეა დამრგვალებული (მაქს. ცდომილება 5e-7), ამიტომ 1e-5
    //დაშვება დამრგვალების სხვაობას ფარავს, რეალურად განსხვავებულ წერტილებს კი ვერ შეაწებებს
    private const double DuplicateToleranceDegrees = 1e-5;

    private static bool IsValidCoordinatePair(double latitude, double longitude)
    {
        return latitude is >= MinValidLatitude and <= MaxValidLatitude &&
               longitude is >= MinValidLongitude and <= MaxValidLongitude;
    }

    //ორი წყარო ერთმანეთს ავსებს: JSON-LD მხოლოდ პირველ ლოკაციას შეიცავს, მაგრამ სრული სიზუსტით,
    //ხილული სია — ყველა ლოკაციას, ოღონდ დამრგვალებულს. ჯერ JSON-LD-ის ვალიდური წყვილები შედის,
    //შემდეგ DOM-ის ისინი, რომლებსაც უკვე შესულებში დაშვების ფარგლებში შესატყვისი არ აქვთ —
    //ტოლერანსი DOM-ის ურთიერთახლო წყვილებსაც აწებებს (~1 მ-ში — ფიზიკურად იგივე წერტილია)
    private static List<PlaceLocationItem> BuildLocations(List<PlaceLocationItem> jsonLdLocations,
        List<PlaceLocationItem> domLocations)
    {
        List<PlaceLocationItem> locations = [];
        foreach (PlaceLocationItem item in jsonLdLocations
                     .Where(w => IsValidCoordinatePair(w.Latitude, w.Longitude))
                     .Where(w => !locations.Contains(w)))
        {
            locations.Add(item);
        }

        foreach (PlaceLocationItem item in domLocations
                     .Where(w => IsValidCoordinatePair(w.Latitude, w.Longitude))
                     .Where(w => !locations.Any(a =>
                         Math.Abs(a.Latitude - w.Latitude) <= DuplicateToleranceDegrees &&
                         Math.Abs(a.Longitude - w.Longitude) <= DuplicateToleranceDegrees)))
        {
            locations.Add(item);
        }

        return locations;
    }

    private static List<PlaceLocationItem> ParseDomLocations(IHtmlDocument document)
    {
        IElement? locationItem = document.QuerySelectorAll(".technical-details .item")
            .FirstOrDefault(f => f.QuerySelector("span.icon-location") is not null);
        if (locationItem is null)
        {
            return [];
        }

        //რამდენიმე ლოკაციისას წყვილები ქვესიის ელემენტებშია, ერთისას — პირდაპირ content ელემენტში
        List<PlaceLocationItem> locations = [];
        IHtmlCollection<IElement> subItems = locationItem.QuerySelectorAll("ul.sub-content li");
        if (subItems.Length == 0)
        {
            PlaceLocationItem? contentPair = TryParsePair(locationItem.QuerySelector("div.content")?.TextContent);
            if (contentPair is not null)
            {
                locations.Add(contentPair);
            }

            return locations;
        }

        foreach (PlaceLocationItem pair in subItems.Select(s => TryParsePair(s.TextContent)).OfType<PlaceLocationItem>())
        {
            locations.Add(pair);
        }

        return locations;
    }

    private static PlaceLocationItem? TryParsePair(string? text)
    {
        if (text is null)
        {
            return null;
        }

        List<double> numbers = [];
        foreach (string part in text.Split(','))
        {
            if (double.TryParse(part.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
            {
                numbers.Add(number);
            }
        }

        return numbers.Count == 2 ? new PlaceLocationItem(numbers[0], numbers[1]) : null;
    }

    private static List<PlaceDistanceItem> ParseDistances(IHtmlDocument document)
    {
        IElement? distancesItem = document.QuerySelectorAll(".technical-details .item")
            .FirstOrDefault(f => f.QuerySelector("span.icon-map-signs") is not null);
        if (distancesItem is null)
        {
            return [];
        }

        //.NET-ის \s არასამტეხლო ჰარსაც (&nbsp;, U+00A0) ფარავს, ამიტომ მისი ცალკე ჩანაცვლება საჭირო არ არის
        List<PlaceDistanceItem> distances = [];
        foreach (IElement item in distancesItem.QuerySelectorAll("ul.sub-content li"))
        {
            //ჩანაწერის ფორმაა „93კმ თბილისიდან" — რიცხვს „კმ" მიწებებული მოსდევს, შემდეგ საწყისი წერტილის სახელი;
            //ამ ფორმას აცდენილი ჩანაწერები გამოიტოვება
            string text = WhitespaceRegex().Replace(item.TextContent, " ").Trim();
            Match match = DistanceItemRegex().Match(text);
            if (match.Success)
            {
                distances.Add(new PlaceDistanceItem(match.Groups["name"].Value,
                    int.Parse(match.Groups["km"].Value, CultureInfo.InvariantCulture)));
            }
        }

        return distances;
    }

    private static string? ParseDescription(IHtmlDocument document)
    {
        IElement? editor = document.QuerySelector(".collapsed-text .editor-text") ??
                           document.QuerySelector(".editor-text");
        if (editor is null)
        {
            return null;
        }

        //TextContent ბლოკებს შორის საზღვრებს კარგავს, ამიტომ ტექსტი ბლოკური ელემენტების მიხედვით ხაზ-ხაზ იკრიბება
        var builder = new StringBuilder();
        AppendNodeText(editor, builder);

        List<string> lines = [];
        foreach (string line in builder.ToString().Split('\n'))
        {
            string cleanLine = WhitespaceRegex().Replace(line, " ").Trim();
            if (cleanLine.Length > 0)
            {
                lines.Add(cleanLine);
            }
        }

        return lines.Count == 0 ? null : string.Join('\n', lines);
    }

    private static void AppendNodeText(INode node, StringBuilder builder)
    {
        foreach (INode child in node.ChildNodes)
        {
            if (child is IText text)
            {
                //წყაროს ტექსტში ხაზგადატანები ფორმატირების ნაწილია და ჰარებად ჩამოიშლება —
                //ახალი ხაზი მხოლოდ ბლოკური ელემენტების საზღვრებზე ჩაისმის
                builder.Append(WhitespaceRegex().Replace(text.Data, " "));
                continue;
            }

            if (child is not IElement element)
            {
                continue;
            }

            if (string.Equals(element.LocalName, "br", StringComparison.OrdinalIgnoreCase))
            {
                builder.Append('\n');
                continue;
            }

            AppendNodeText(element, builder);
            if (BlockTagNames.Contains(element.LocalName))
            {
                builder.Append('\n');
            }
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        return value is null || value.Length <= maxLength ? value : value[..maxLength];
    }

    [GeneratedRegex(@"^(?<km>\d+)\s*კმ\.?\s*(?<name>.+)$")]
    private static partial Regex DistanceItemRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    //JSON-LD ბლოკებიდან ამოკრებილი მონაცემები; გვიანდელი კვანძები ადრინდელებს გადაფარავს, როგორც ძველ
    //JS-სკრიპტში — გარდა ლოკაციებისა, რომლებიც ყველა კვანძიდან გროვდება
    private sealed class JsonLdData
    {
        public bool IsTouristAttraction { get; private set; }
        public string? Name { get; private set; }
        public List<PlaceLocationItem> Locations { get; } = [];
        public string? Region { get; private set; }
        public string? Municipality { get; private set; }

        public static JsonLdData Parse(IHtmlDocument document)
        {
            var data = new JsonLdData();
            foreach (IElement script in document.QuerySelectorAll("script[type='application/ld+json']"))
            {
                data.ParseScript(script.TextContent);
            }

            return data;
        }

        private void ParseScript(string scriptText)
        {
            JsonDocument jsonDocument;
            try
            {
                jsonDocument = JsonDocument.Parse(scriptText);
            }
            catch (JsonException)
            {
                //არავალიდური JSON-LD ბლოკი უბრალოდ გამოიტოვება
                return;
            }

            using (jsonDocument)
            {
                foreach (JsonElement node in EnumerateGraphNodes(jsonDocument.RootElement))
                {
                    ApplyNode(node);
                }
            }
        }

        private static List<JsonElement> EnumerateGraphNodes(JsonElement root)
        {
            if (root.ValueKind == JsonValueKind.Array)
            {
                return [.. root.EnumerateArray()];
            }

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("@graph", out JsonElement graph) &&
                graph.ValueKind == JsonValueKind.Array)
            {
                return [.. graph.EnumerateArray()];
            }

            return [root];
        }

        private void ApplyNode(JsonElement node)
        {
            if (node.ValueKind != JsonValueKind.Object || !node.TryGetProperty("@type", out JsonElement typeElement))
            {
                return;
            }

            if (HasType(typeElement, "TouristAttraction"))
            {
                ApplyTouristAttraction(node);
            }

            if (HasType(typeElement, "BreadcrumbList"))
            {
                ApplyBreadcrumbs(node);
            }
        }

        private static bool HasType(JsonElement typeElement, string typeName)
        {
            return typeElement.ValueKind switch
            {
                JsonValueKind.String => typeElement.ValueEquals(typeName),
                JsonValueKind.Array => typeElement.EnumerateArray()
                    .Any(a => a.ValueKind == JsonValueKind.String && a.ValueEquals(typeName)),
                _ => false
            };
        }

        private void ApplyTouristAttraction(JsonElement node)
        {
            IsTouristAttraction = true;

            if (node.TryGetProperty("name", out JsonElement nameElement) &&
                nameElement.ValueKind == JsonValueKind.String)
            {
                string? attractionName = nameElement.GetString();
                if (!string.IsNullOrEmpty(attractionName))
                {
                    Name = attractionName;
                }
            }

            if (!node.TryGetProperty("geo", out JsonElement geo))
            {
                return;
            }

            //geo ჩვეულებრივ ერთი ობიექტია, მაგრამ schema.org მასივსაც უშვებს — ორივე ფორმა იკითხება
            if (geo.ValueKind == JsonValueKind.Object)
            {
                TryAddGeoPair(geo);
            }
            else if (geo.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement geoItem in geo.EnumerateArray()
                             .Where(w => w.ValueKind == JsonValueKind.Object))
                {
                    TryAddGeoPair(geoItem);
                }
            }
        }

        private void TryAddGeoPair(JsonElement geo)
        {
            //წყვილი მხოლოდ მაშინ ემატება, როცა ორივე კოორდინატი რიცხვია
            if (geo.TryGetProperty("latitude", out JsonElement latitude) &&
                latitude.ValueKind == JsonValueKind.Number &&
                geo.TryGetProperty("longitude", out JsonElement longitude) &&
                longitude.ValueKind == JsonValueKind.Number)
            {
                Locations.Add(new PlaceLocationItem(latitude.GetDouble(), longitude.GetDouble()));
            }
        }

        private void ApplyBreadcrumbs(JsonElement node)
        {
            if (!node.TryGetProperty("itemListElement", out JsonElement items) ||
                items.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            //პირველი ორი ელემენტი (მთავარი გვერდი და კატეგორია) და ბოლო (თვითონ ადგილი) გამოიტოვება —
            //შუაში რეგიონი და მუნიციპალიტეტია
            List<string> middleNames = [];
            int count = items.GetArrayLength();
            for (var i = 2; i < count - 1; i++)
            {
                JsonElement item = items[i];
                if (item.ValueKind == JsonValueKind.Object &&
                    item.TryGetProperty("name", out JsonElement nameElement) &&
                    nameElement.ValueKind == JsonValueKind.String)
                {
                    string? itemName = nameElement.GetString();
                    if (!string.IsNullOrEmpty(itemName))
                    {
                        middleNames.Add(itemName);
                    }
                }
            }

            if (middleNames.Count > 0)
            {
                Region = middleNames[0];
            }

            if (middleNames.Count > 1)
            {
                Municipality = middleNames[1];
            }
        }
    }
}
