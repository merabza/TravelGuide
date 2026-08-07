namespace TravelGuide.Runners;

//გვერდიდან ამოღებული ერთი მანძილის ჩანაწერი: საწყისი წერტილის სახელი ისე, როგორც საიტზე წერია
//(მაგ. „თბილისიდან", „ქუთაისის საერთაშორისო აეროპორტიდან") და მანძილი კილომეტრებში
public sealed record PlaceDistanceItem(string FromPointName, int DistanceKm);
