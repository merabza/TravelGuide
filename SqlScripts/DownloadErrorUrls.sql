-- შეცდომიანი (DownloadError) გვერდები და წყარო გვერდები, რომლებზეც მათი მისამართები მოიძებნა.
-- State მნიშვნელობები (EState enum, int-ად ინახება):
--   0=New, 1=Opening, 2=Opened, 3=Analysing, 4=Analysed, 5=NotAttraction, 6=DownloadError
--
-- UrlGraphNodes-ში წიბო ნიშნავს: FromUrlId გვერდზე მოიძებნა GotUrlId მისამართი.
-- LEFT JOIN იმიტომ, რომ sitemap-იდან მოსულ ან საწყის წერტილად ჩასმულ მისამართებს
-- წყარო გვერდი ჩაწერილი არ აქვთ — ისინიც გამოჩნდეს, FoundOnUrl = NULL მნიშვნელობით.

SELECT ep.PlaceId AS ErrorPlaceId,
       ep.Url     AS ErrorUrl,
       src.Url    AS FoundOnUrl
FROM dbo.Places ep
    LEFT JOIN dbo.UrlGraphNodes ugn ON ugn.GotUrlId = ep.PlaceId
    LEFT JOIN dbo.Places src ON src.PlaceId = ugn.FromUrlId
WHERE ep.State = 6
ORDER BY ep.Url, src.Url;
