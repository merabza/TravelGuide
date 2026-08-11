namespace TravelGuide.Runners;

//გვერდიდან ამოღებული ერთი ლოკაციის კოორდინატები; record-ის მნიშვნელობითი ტოლობა
//ზუსტი დუბლიკატების გასაფილტრად გამოიყენება
public sealed record PlaceLocationItem(double Latitude, double Longitude);
