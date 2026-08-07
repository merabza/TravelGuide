namespace TravelGuideDbModels;

public enum EState
{
    //ჩამოსატვირთი: მისამართი შეგროვებულია და გვერდის ჩამოტვირთვა-გაანალიზება ელოდება
    New,
    Opening,
    Opened,
    Analysing,
    Analysed,

    //გვერდი მოქაჩვისას ღირსშესანიშნაობის გვერდი არ აღმოჩნდა (sitemap-ში ქალაქების/რეგიონების გვერდებიც ხვდება)
    NotAttraction
}
