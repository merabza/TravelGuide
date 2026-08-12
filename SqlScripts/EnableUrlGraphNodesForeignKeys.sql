-- UrlGraphNodes-ის ორივე რელაციური კავშირი (FromUrlId→Places, GotUrlId→Places) ბაზაში
-- გამორთულია ან WITH NOCHECK-ით არის შექმნილი ("Check Existing Data On Creation Or Re-Enabling" = No),
-- ამიტომ ჩანაწერის ჩამატებისას ბაზა იდენტიფიკატორების სისწორეს არ ამოწმებს.
-- ეს სკრიპტი ორივე კავშირს რთავს არსებული მონაცემების შემოწმებით — შედეგად ჩამატება/განახლება
-- მოწმდება, რომ FromUrlId და GotUrlId ნამდვილად არსებობდეს Places ცხრილში.

-- 1. ობოლი ჩანაწერები: მითითებული PlaceId, რომელსაც Places-ში შესატყვისი არ აქვს.
--    ასეთი რიგების არსებობისას მე-3 ნაბიჯი შეცდომით დასრულდება — ჯერ ისინი უნდა წაიშალოს (მე-2 ნაბიჯი).
SELECT ugn.*
FROM dbo.UrlGraphNodes ugn
WHERE NOT EXISTS (SELECT 1 FROM dbo.Places p WHERE p.PlaceId = ugn.FromUrlId)
   OR NOT EXISTS (SELECT 1 FROM dbo.Places p WHERE p.PlaceId = ugn.GotUrlId);

-- 2. მხოლოდ მაშინ გაეშვას, თუ პირველმა ნაბიჯმა რიგები დააბრუნა (ობოლი წიბო გრაფში გამოუსადეგარია):
--DELETE ugn
--FROM dbo.UrlGraphNodes ugn
--WHERE NOT EXISTS (SELECT 1 FROM dbo.Places p WHERE p.PlaceId = ugn.FromUrlId)
--   OR NOT EXISTS (SELECT 1 FROM dbo.Places p WHERE p.PlaceId = ugn.GotUrlId);

-- 3. ცხრილის ყველა კავშირის ჩართვა არსებული მონაცემების შემოწმებით (კავშირების სახელებზე არ არის დამოკიდებული).
ALTER TABLE dbo.UrlGraphNodes WITH CHECK CHECK CONSTRAINT ALL;

-- 4. შედეგი: ორივე კავშირზე is_disabled = 0 და is_not_trusted = 0 უნდა იყოს.
SELECT fk.name, fk.is_disabled, fk.is_not_trusted
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.UrlGraphNodes');

-- სურვილისამებრ: მთელ ბაზაში გამორთული ან შეუმოწმებელი კავშირების სია —
-- მონაცემების გადმოტანა (SqlBulkCopy) ყველა ჩატვირთული ცხრილის კავშირს untrusted-ად ტოვებს, ამიტომ სხვებიც აქ გამოჩნდება.
--SELECT OBJECT_NAME(fk.parent_object_id) AS TableName, fk.name, fk.is_disabled, fk.is_not_trusted
--FROM sys.foreign_keys fk
--WHERE fk.is_disabled = 1 OR fk.is_not_trusted = 1
--ORDER BY TableName, fk.name;

-- მთელი ბაზის გამოსწორება ერთიანად — ყველა ცხრილის ყველა კავშირის ჩართვა არსებული მონაცემების შემოწმებით:
--EXEC sys.sp_msforeachtable N'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';
