namespace TravelGuideDbModels;

public enum EState
{
    New,
    Opening,
    Opened,
    Analysing,
    Analysed,

    //გვერდი მოქაჩვისას ღირსშესანიშნაობის გვერდი არ აღმოჩნდა (sitemap-ში ქალაქების/რეგიონების გვერდებიც ხვდება)
    NotAttraction
}
