using System;
using System.Collections.Generic;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;

namespace TravelGuide.Runners;

//გვერდის HTML-იდან ბმულების ამოკრება — ანალიზის ფაზა ამით პოულობს ახალ მისამართებს, რომ ისინიც
//ჩამოსატვირთების რიგში ჩადგეს (საწყისი წერტილების მსგავსობას HarvestedUrlPersister ამოწმებს)
public static class PageLinkExtractor
{
    public static List<string> ExtractLinks(IHtmlDocument document, Uri pageUri)
    {
        //HashSet ერთი გვერდის შიგნით განმეორებულ ბმულებს ფილტრავს
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (IElement anchor in document.QuerySelectorAll("a[href]"))
        {
            string? href = anchor.GetAttribute("href");

            //ფარდობითი ბმულები გვერდის მისამართის მიმართ აბსოლუტურდება
            if (string.IsNullOrWhiteSpace(href) || !Uri.TryCreate(pageUri, href, out Uri? absolute))
            {
                continue;
            }

            //mailto:/tel:/javascript: ბმულები გამოიტოვება
            if (absolute.Scheme != Uri.UriSchemeHttp && absolute.Scheme != Uri.UriSchemeHttps)
            {
                continue;
            }

            //query-იანი მისამართები (ძებნა/ფილტრები) კონტენტის გვერდები არ არის და უსასრულო ვარიაციებს წარმოშობს
            if (absolute.Query.Length > 0)
            {
                continue;
            }

            //fragment იჭრება — ის იმავე გვერდის ნაწილია
            result.Add(absolute.GetLeftPart(UriPartial.Path));
        }

        return [.. result];
    }
}
