-- შეცდომიანი (DownloadError) გვერდები და წყარო გვერდები, რომლებზეც მათი მისამართები მოიძებნა.
-- State მნიშვნელობები (EState enum, int-ად ინახება):
--   0=New, 1=Opening, 2=Opened, 3=Analysing, 4=Analysed, 5=NotAttraction, 6=DownloadError, 7=Duplicate
--
-- მისამართები Urls ცხრილშია (Places.UrlId → Urls.UrlId). UrlGraphNodes-ში წიბო ნიშნავს: FromUrlId მისამართის
-- გვერდზე მოიძებნა GotUrlId მისამართი — ორივე Urls-ის ჩანაწერია.
-- LEFT JOIN იმიტომ, რომ sitemap-იდან მოსულ ან საწყის წერტილად ჩასმულ მისამართებს
-- წყარო გვერდი ჩაწერილი არ აქვთ — ისინიც გამოჩნდეს, FoundOnUrl = NULL მნიშვნელობით.

SELECT ep.PlaceId AS ErrorPlaceId,
       eu.Url     AS ErrorUrl,
       su.Url     AS FoundOnUrl
FROM dbo.Places ep
    JOIN dbo.Urls eu ON eu.UrlId = ep.UrlId
    LEFT JOIN dbo.UrlGraphNodes ugn ON ugn.GotUrlId = ep.UrlId
    LEFT JOIN dbo.Urls su ON su.UrlId = ugn.FromUrlId
WHERE ep.State = 6
ORDER BY eu.Url, su.Url;
