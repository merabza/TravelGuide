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
    //JSON-LD პირველადი წყაროა (კოორდინატები სრული სიზუსტით), ხილული DOM — დანარჩენი ველებისთვის და fallback-ად.
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

        double? latitude = jsonLd.Latitude;
        double? longitude = jsonLd.Longitude;
        if (latitude is null || longitude is null)
        {
            (double, double)? coordinates = ParseCoordinatesFallback(document);
            if (coordinates is not null)
            {
                (latitude, longitude) = coordinates.Value;
            }
        }

        return new PlaceExtractResult
        {
            IsTouristAttraction = jsonLd.IsTouristAttraction,
            Name = name,
            Latitude = latitude,
            Longitude = longitude,
            Region = region,
            Municipality = municipality,
            Categories = SelectTexts(document, ".destination-header .categories a"),
            Tags = SelectTexts(document, "section.tags a"),
            BestSeason = TrimmedTextOrNull(document.QuerySelector(".best-time-to-visit strong")),
            Distances = ParseDistances(document),
            Description = ParseDescription(document)
        };
    }

    public static void Apply(PlaceExtractResult extract, PlaceModel place)
    {
        place.Name = Truncate(extract.Name, PlaceModelConfiguration.NameLength);
        place.Latitude = extract.Latitude;
        place.Longitude = extract.Longitude;
        place.Region = Truncate(extract.Region, PlaceModelConfiguration.RegionLength);
        place.Municipality = Truncate(extract.Municipality, PlaceModelConfiguration.MunicipalityLength);
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

    private static (double, double)? ParseCoordinatesFallback(IHtmlDocument document)
    {
        IElement? content = document.QuerySelectorAll(".technical-details .item")
            .FirstOrDefault(f => f.QuerySelector("span.icon-location") is not null)?.QuerySelector("div.content");
        if (content is null)
        {
            return null;
        }

        List<double> numbers = [];
        foreach (string part in content.TextContent.Split(','))
        {
            if (double.TryParse(part.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
            {
                numbers.Add(number);
            }
        }

        return numbers.Count == 2 ? (numbers[0], numbers[1]) : null;
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

    //JSON-LD ბლოკებიდან ამოკრებილი მონაცემები; გვიანდელი კვანძები ადრინდელებს გადაფარავს, როგორც ძველ JS-სკრიპტში
    private sealed class JsonLdData
    {
        public bool IsTouristAttraction { get; private set; }
        public string? Name { get; private set; }
        public double? Latitude { get; private set; }
        public double? Longitude { get; private set; }
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

            if (!node.TryGetProperty("geo", out JsonElement geo) || geo.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            if (geo.TryGetProperty("latitude", out JsonElement latitude) &&
                latitude.ValueKind == JsonValueKind.Number)
            {
                Latitude = latitude.GetDouble();
            }

            if (geo.TryGetProperty("longitude", out JsonElement longitude) &&
                longitude.ValueKind == JsonValueKind.Number)
            {
                Longitude = longitude.GetDouble();
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
