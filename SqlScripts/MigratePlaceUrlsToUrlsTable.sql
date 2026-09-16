-- ადგილების მისამართების გადატანა Places ცხრილიდან ახალ Urls ცხრილში (Places.Url, Places.UrlHashCode →
-- Urls.Url, Urls.UrlHashCode; ადგილს ახალი Places.UrlId → Urls.UrlId სვეტი ემატება) და ბმულების გრაფის
-- (UrlGraphNodes.FromUrlId / GotUrlId) გადაბმა Places-იდან Urls-ზე. ადგილს მისამართი შეიძლება არ ჰქონდეს
-- (ხელით შეყვანილი), გრაფი კი მისამართებს შორის კავშირებს ინახავს — ამიტომ მისამართი ცალკე ერთეულია.
--
-- სკრიპტი არსებულ ბაზას სტრუქტურას ისე უცვლის, რომ მონაცემი არ იკარგება, და ზუსტად იმ სქემას იძლევა, რასაც EF-ის
-- მიგრაცია 20260916064238_UrlsTable (TravelGuideDbTools) — ბოლოს მიგრაცია __EFMigrationsHistory-ში
-- შესრულებულად ინიშნება, რომ `dotnet ef database update` მას ხელახლა არ ცადოს.
--
-- მთავარი ხერხი: Urls-ის ჩანაწერს იმ ადგილის PlaceId ენიჭება UrlId-ად, რომლისგანაც გადმოვიდა (IDENTITY_INSERT).
-- ამით UrlGraphNodes-ის FromUrlId/GotUrlId მნიშვნელობები, რომლებიც აქამდე PlaceId-ები იყო, უცვლელად სწორი
-- UrlId-ები ხდება და გრაფის გადაწერა საჭირო არ არის; ახალი მისამართები იდენტიფიკატორებს მაქსიმუმის შემდეგ მიიღებს.
--
-- ნაბიჯები თანმიმდევრობით გაეშვას (მთელი სკრიპტიც ერთიანად გაეშვება — ბატჩები GO-თია გამოყოფილი). მე-3 ნაბიჯი
-- (სქემის დასრულება) ერთ ტრანზაქციაშია და ნებისმიერი შეცდომისას მთლიანად უკან ბრუნდება; მანამდე დამატებული
-- Urls ცხრილი და Places.UrlId სვეტი უსაფრთხოა — ძველი სვეტები მე-3 ნაბიჯამდე არ იშლება.
-- სარეზერვო ასლი საჭიროებისას:
--SELECT * INTO dbo.Places_bak_BeforeUrls FROM dbo.Places;
--SELECT * INTO dbo.UrlGraphNodes_bak_BeforeUrls FROM dbo.UrlGraphNodes;

-- 0. წინასწარი შემოწმებები — ორივე მოთხოვნა ცარიელი უნდა იყოს.
--    ა) მისამართიანი ადგილი ხეშ-კოდის გარეშე: Urls-ში UrlHashCode სავალდებულოა (აპლიკაცია ორივეს ერთად წერს,
--       ასეთი ჩანაწერი მონაცემების შეცდომაა და ხეშ-კოდი ხელით უნდა შეივსოს).
SELECT PlaceId, Url FROM dbo.Places WHERE Url IS NOT NULL AND UrlHashCode IS NULL;

--    ბ) გრაფის წიბოები, რომლებიც უმისამართო (ან არარსებულ) ადგილზე მიუთითებს — Urls-ში მათი შესატყვისი არ იქნება
--       და მე-3 ნაბიჯის კავშირი ვერ შეიქმნება; ასეთი წიბო გრაფში უაზროა და უნდა წაიშალოს (ქვემოთ დაკომენტარებული DELETE).
SELECT ugn.*
FROM dbo.UrlGraphNodes ugn
WHERE NOT EXISTS (SELECT 1 FROM dbo.Places p WHERE p.PlaceId = ugn.FromUrlId AND p.Url IS NOT NULL)
   OR NOT EXISTS (SELECT 1 FROM dbo.Places p WHERE p.PlaceId = ugn.GotUrlId AND p.Url IS NOT NULL);

--DELETE ugn
--FROM dbo.UrlGraphNodes ugn
--WHERE NOT EXISTS (SELECT 1 FROM dbo.Places p WHERE p.PlaceId = ugn.FromUrlId AND p.Url IS NOT NULL)
--   OR NOT EXISTS (SELECT 1 FROM dbo.Places p WHERE p.PlaceId = ugn.GotUrlId AND p.Url IS NOT NULL);
GO

-- 1. ახალი ცხრილი და სვეტი EF-ის მიგრაციის სახელებით (PK_Urls, IX_Urls_UrlHashCode). განმეორებით გაშვებისას
--    არსებული ხელახლა არ იქმნება.
IF OBJECT_ID(N'dbo.Urls', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Urls
    (
        UrlId       int           IDENTITY (1, 1) NOT NULL,
        Url         nvarchar(500) NOT NULL,
        UrlHashCode int           NOT NULL,
        CONSTRAINT PK_Urls PRIMARY KEY CLUSTERED (UrlId)
    );
    CREATE INDEX IX_Urls_UrlHashCode ON dbo.Urls (UrlHashCode);
END;
IF COL_LENGTH(N'dbo.Places', N'UrlId') IS NULL
    ALTER TABLE dbo.Places ADD UrlId int NULL;
GO

-- 2. მონაცემების გადატანა: მისამართიანი ადგილების Url/UrlHashCode Urls-ში იწერება UrlId = PlaceId-ით და ადგილს
--    UrlId ენიჭება. განმეორებით გაშვებისას უკვე გადატანილი არ ორმაგდება.
IF EXISTS (SELECT 1 FROM dbo.Places WHERE Url IS NOT NULL AND UrlHashCode IS NULL)
    RAISERROR (N'Places rows with Url but without UrlHashCode exist - see step 0', 16, 1);
ELSE
BEGIN
    SET IDENTITY_INSERT dbo.Urls ON;

    INSERT INTO dbo.Urls (UrlId, Url, UrlHashCode)
    SELECT p.PlaceId, p.Url, p.UrlHashCode
    FROM dbo.Places p
    WHERE p.Url IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.Urls u WHERE u.UrlId = p.PlaceId);

    SET IDENTITY_INSERT dbo.Urls OFF;

    UPDATE dbo.Places SET UrlId = PlaceId WHERE Url IS NOT NULL AND UrlId IS NULL;
END;
GO

-- 3. სქემის დასრულება ერთ ტრანზაქციაში: კავშირები Places-ის ნაცვლად Urls-ზე გადადის (EF-ის სახელებით:
--    FK_Places_Urls_UrlId, IX_Places_UrlId, FK_UrlGraphNodes_Urls_FromUrlId, FK_UrlGraphNodes_Urls_GotUrlId),
--    ძველი კავშირები (FK_UrlGraphNodes_Places_FromUrlId / _GotUrlId), ინდექსი IX_Places_UrlHashCode და სვეტები
--    Url, UrlHashCode იშლება. გადაუტანელი მისამართის ან ობოლი წიბოს არსებობისას ბატჩი შეცდომით ჩერდება და
--    ყველაფერი უკან ბრუნდება (XACT_ABORT).
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM dbo.Places WHERE Url IS NOT NULL AND UrlId IS NULL)
    THROW 50000, N'Places rows with Url but without UrlId remain - finish step 2 first', 1;
IF EXISTS (SELECT 1
           FROM dbo.UrlGraphNodes ugn
           WHERE NOT EXISTS (SELECT 1 FROM dbo.Urls u WHERE u.UrlId = ugn.FromUrlId)
              OR NOT EXISTS (SELECT 1 FROM dbo.Urls u WHERE u.UrlId = ugn.GotUrlId))
    THROW 50000, N'UrlGraphNodes rows without a matching Urls row remain - see step 0', 1;

ALTER TABLE dbo.UrlGraphNodes DROP CONSTRAINT FK_UrlGraphNodes_Places_FromUrlId;
ALTER TABLE dbo.UrlGraphNodes DROP CONSTRAINT FK_UrlGraphNodes_Places_GotUrlId;

CREATE INDEX IX_Places_UrlId ON dbo.Places (UrlId);
ALTER TABLE dbo.Places WITH CHECK ADD CONSTRAINT FK_Places_Urls_UrlId
    FOREIGN KEY (UrlId) REFERENCES dbo.Urls (UrlId);
ALTER TABLE dbo.UrlGraphNodes WITH CHECK ADD CONSTRAINT FK_UrlGraphNodes_Urls_FromUrlId
    FOREIGN KEY (FromUrlId) REFERENCES dbo.Urls (UrlId);
ALTER TABLE dbo.UrlGraphNodes WITH CHECK ADD CONSTRAINT FK_UrlGraphNodes_Urls_GotUrlId
    FOREIGN KEY (GotUrlId) REFERENCES dbo.Urls (UrlId);

DROP INDEX IX_Places_UrlHashCode ON dbo.Places;
ALTER TABLE dbo.Places DROP COLUMN Url, UrlHashCode;

COMMIT TRANSACTION;
GO

-- 4. მიგრაციის შესრულებულად მონიშვნა EF-ისთვის — ბაზა ახლა ზუსტად ისეთია, როგორსაც 20260916064238_UrlsTable მიგრაცია
--    აკეთებს, და `dotnet ef database update` მას აღარ უნდა ცდილობდეს. მიგრაციების ცხრილის გარეშე ბაზაზე არაფერს აკეთებს.
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory WHERE MigrationId = N'20260916064238_UrlsTable')
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES (N'20260916064238_UrlsTable', N'10.0.12');
GO

-- 5. შედეგი: Urls-ის რაოდენობა მისამართიანი ადგილების რაოდენობას უნდა ემთხვეოდეს, გრაფის წიბოების რაოდენობა —
--    სკრიპტამდელს; Urls-ზე მიმართულ სამივე კავშირზე is_disabled = 0 და is_not_trusted = 0 უნდა იყოს.
SELECT (SELECT COUNT(*) FROM dbo.Urls)                                AS UrlsCount,
       (SELECT COUNT(*) FROM dbo.Places WHERE UrlId IS NOT NULL)      AS PlacesWithUrlCount,
       (SELECT COUNT(*) FROM dbo.Places WHERE UrlId IS NULL)          AS PlacesWithoutUrlCount,
       (SELECT COUNT(*) FROM dbo.UrlGraphNodes)                       AS UrlGraphNodesCount;
SELECT OBJECT_NAME(fk.parent_object_id) AS TableName, fk.name, fk.is_disabled, fk.is_not_trusted
FROM sys.foreign_keys fk
WHERE fk.referenced_object_id = OBJECT_ID(N'dbo.Urls')
ORDER BY TableName, fk.name;
