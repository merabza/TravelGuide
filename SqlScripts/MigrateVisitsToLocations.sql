-- ვიზიტების გადაბმა Places-იდან Locations-ზე (Visits.PlaceId → Visits.LocationId).
-- ვიზიტი ლოკაციაზე ფიქსირდება და არა ადგილზე — ადგილს რამდენიმე ლოკაცია შეიძლება ჰქონდეს და ერთის
-- მონახულება დანარჩენებს ნამყოფად არ უნდა ნიშნავდეს.
--
-- სად და როდის: სქემის სინქრონიზაციამდე, იმ ბაზაზე, საიდანაც მონაცემები გადაიტანება (TravelGuideProdCopy —
-- Development-იდან განახლების შემდეგ). სკრიპტის შემდეგ Visits ცხრილს PlaceId აღარ აქვს და LocationId აქვს,
-- ანუ ზუსტად ის სვეტები, რაც ახალ მოდელს — გადამტანი ხელსაწყო (TransferProdCopyToDevByPairs) სვეტებს
-- სახელით აწყვილებს. ძველი pairs ფაილი (PairedDbObjectsResults\TravelGuide.json) Visits-ისთვის
-- „PlaceId - PlaceId" წყვილს შეიცავს და ვალიდაციაზე ჩავარდება — ფაილი უნდა წაიშალოს (ხელსაწყო თავიდან
-- დააგენერირებს) ან ეს წყვილი „LocationId - LocationId"-ით შეიცვალოს.
--
-- რომელი ვიზიტი გადადის ავტომატურად: რომლის ადგილსაც ზუსტად ერთი ლოკაცია აქვს (PlacesByLocations) — ვიზიტი
-- ამ ლოკაციას ებმება. მრავალლოკაციიანი ან ულოკაციო ადგილის ვიზიტს ლოკაცია ხელით უნდა მიეთითოს (მე-4 ნაბიჯი),
-- თორემ სურათებთან ერთად წაიშლება (მე-5 ნაბიჯი). ულოკაციო ადგილს ლოკაცია აპლიკაციის Places რედაქტორით
-- (Locations → New Location) შეიძლება დაემატოს — ProdCopy-ის განახლებამდე, Development ბაზაზე.
--
-- ნაბიჯები თანმიმდევრობით გაეშვას, არა მთელი სკრიპტი ერთდროულად — მე-4 და მე-5 ნაბიჯები განზრახ არის
-- დაკომენტარებული, მე-6 კი გადაუყვანელი ვიზიტების არსებობისას შეცდომით ჩერდება.

-- 1. ახალი სვეტი — ჯერ NULL-ის დაშვებით, სანამ ყველა ვიზიტს ლოკაცია არ ექნება.
IF COL_LENGTH(N'dbo.Visits', N'LocationId') IS NULL
    ALTER TABLE dbo.Visits ADD LocationId int NULL;
GO

-- 2. ერთლოკაციიანი ადგილების ვიზიტებს ლოკაცია ავტომატურად ენიჭება.
UPDATE v
SET v.LocationId = pl.LocationId
FROM dbo.Visits v
    JOIN dbo.PlacesByLocations pl ON pl.PlaceId = v.PlaceId
WHERE v.LocationId IS NULL
  AND (SELECT COUNT(*) FROM dbo.PlacesByLocations pl2 WHERE pl2.PlaceId = v.PlaceId) = 1;

-- 3. გადაუყვანელი ვიზიტები: ადგილი, მისი ლოკაციების რაოდენობა და ვიზიტის სურათების რაოდენობა.
SELECT v.VisitId, v.VisitDate, v.PlaceId, p.Name AS PlaceName, v.Comment,
       (SELECT COUNT(*) FROM dbo.PlacesByLocations pl WHERE pl.PlaceId = v.PlaceId) AS LocationsCount,
       (SELECT COUNT(*) FROM dbo.VisitImages vi WHERE vi.VisitId = v.VisitId) AS ImagesCount
FROM dbo.Visits v
    JOIN dbo.Places p ON p.PlaceId = v.PlaceId
WHERE v.LocationId IS NULL
ORDER BY v.VisitDate, v.VisitId;

--    გადაუყვანელი ვიზიტების ადგილების ლოკაციები — LocationId ხელით მისათითებლად (მე-4 ნაბიჯი).
SELECT v.VisitId, v.PlaceId, l.LocationId, l.Latitude, l.Longitude
FROM dbo.Visits v
    JOIN dbo.PlacesByLocations pl ON pl.PlaceId = v.PlaceId
    JOIN dbo.Locations l ON l.LocationId = pl.LocationId
WHERE v.LocationId IS NULL
ORDER BY v.VisitId, l.LocationId;

-- 4. სურვილისამებრ, ხელით: კონკრეტულ ვიზიტს ადგილის ერთ-ერთი ლოკაცია (მე-3 ნაბიჯის მეორე სიიდან) მიეთითება.
--UPDATE dbo.Visits SET LocationId = <LocationId> WHERE VisitId = <VisitId>;

-- 5. დარჩენილი გადაუყვანელი ვიზიტები სურათებთან ერთად იშლება — მხოლოდ მე-3 ნაბიჯის სიის გადახედვისა და
--    მე-4 ნაბიჯის შემდეგ. სარეზერვო ასლი საჭიროებისას:
--SELECT * INTO dbo.Visits_bak_BeforeLocations FROM dbo.Visits;
--SELECT * INTO dbo.VisitImages_bak_BeforeLocations FROM dbo.VisitImages;
--DELETE vi
--FROM dbo.VisitImages vi
--WHERE EXISTS (SELECT 1 FROM dbo.Visits v WHERE v.VisitId = vi.VisitId AND v.LocationId IS NULL);
--DELETE FROM dbo.Visits WHERE LocationId IS NULL;

-- 6. სქემის დასრულება ერთ ბატჩში: LocationId სავალდებულო ხდება, ინდექსი და კავშირი EF-ის სახელებით ემატება
--    (IX_Visits_LocationId, FK_Visits_Locations_LocationId), PlaceId კავშირითა და ინდექსით ერთად იშლება
--    (სახელები FK_Visits_Places_PlaceId / IX_Visits_PlaceId — EF-ის მიგრაციით შექმნილი, ბაზაში შემოწმებული).
--    გადაუყვანელი ვიზიტი რომ დარჩეს, ბატჩი შეცდომით ჩერდება და სქემა უცვლელი რჩება.
IF EXISTS (SELECT 1 FROM dbo.Visits WHERE LocationId IS NULL)
    RAISERROR (N'Visits without LocationId remain - finish steps 3-5 first', 16, 1);
ELSE
BEGIN
    ALTER TABLE dbo.Visits ALTER COLUMN LocationId int NOT NULL;
    CREATE INDEX IX_Visits_LocationId ON dbo.Visits (LocationId);
    ALTER TABLE dbo.Visits WITH CHECK ADD CONSTRAINT FK_Visits_Locations_LocationId
        FOREIGN KEY (LocationId) REFERENCES dbo.Locations (LocationId) ON DELETE CASCADE;
    ALTER TABLE dbo.Visits DROP CONSTRAINT FK_Visits_Places_PlaceId;
    DROP INDEX IX_Visits_PlaceId ON dbo.Visits;
    ALTER TABLE dbo.Visits DROP COLUMN PlaceId;
END
GO

-- 7. შედეგი: ვიზიტების რაოდენობა და კავშირების მდგომარეობა (Locations-ის კავშირზე is_not_trusted = 0 უნდა იყოს).
SELECT COUNT(*) AS VisitsCount FROM dbo.Visits;
SELECT fk.name, OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable, fk.is_disabled, fk.is_not_trusted
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Visits');
