using System;
using System.Collections.Generic;
using SystemTools.SystemToolsShared;

namespace TravelGuide.Runners;

public static class BestSeasonParser
{
    //ინდექსი + 1 = კალენდარული თვის ნომერი; ეს სია Months ცხრილის შესავსებადაც გამოიყენება
    private static readonly string[] MonthNamesArray =
    [
        "იანვარი", "თებერვალი", "მარტი", "აპრილი", "მაისი", "ივნისი",
        "ივლისი", "აგვისტო", "სექტემბერი", "ოქტომბერი", "ნოემბერი", "დეკემბერი"
    ];

    public static IReadOnlyList<string> MonthNames => MonthNamesArray;

    //გამოძახებამდე კონსოლში ისედაც იბეჭდება მიმდინარე ადგილის მისამართი, ამიტომ გაფრთხილებას ის აღარ სჭირდება
    public static List<int> ParseMonths(string? bestSeason)
    {
        if (string.IsNullOrWhiteSpace(bestSeason))
        {
            return [];
        }

        var months = new SortedSet<int>();
        foreach (string token in bestSeason.Split(',',
                     StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            List<int> tokenMonths = ResolveTokenMonths(token);
            if (tokenMonths.Count == 0)
            {
                StShared.WriteErrorLine($"Unknown best season token '{token}'", true, null, false);
                continue;
            }

            months.UnionWith(tokenMonths);
        }

        return [.. months];
    }

    private static List<int> ResolveTokenMonths(string token)
    {
        int monthIndex = Array.IndexOf(MonthNamesArray, token);
        if (monthIndex >= 0)
        {
            return [monthIndex + 1];
        }

        return token switch
        {
            "გაზაფხული" => [3, 4, 5],
            "ზაფხული" => [6, 7, 8],
            "შემოდგომა" => [9, 10, 11],
            "ზამთარი" => [12, 1, 2],
            "ყველა სეზონი" => [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12],
            _ => []
        };
    }
}
