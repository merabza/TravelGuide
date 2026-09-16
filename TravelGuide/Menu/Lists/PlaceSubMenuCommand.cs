using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using TravelGuideCore.Domain.PlaceModels;

namespace TravelGuide.Menu.Lists;

//ადგილის ჩანაწერის მენიუს პუნქტი: სახელი წარწერაა (მშობელი სიის აგებისას დადგენილი, პორციაში უნიკალური),
//რომელიც რედაქტორის გასაღებიც არის; სტატუსში, თუ ადგილს მისამართი აქვს, მისამართის სტატუსი და თავად მისამართი ჩანს
public sealed class PlaceSubMenuCommand : CliMenuCommand
{
    private readonly PlaceCruder _placeCruder;
    private readonly string? _status;

    public PlaceSubMenuCommand(PlaceCruder placeCruder, PlaceModel place, string caption) : base(caption,
        EMenuAction.LoadSubMenu)
    {
        _placeCruder = placeCruder;
        //სტატუსი მისამართისაა (UrlModel.State) — უმისამართო (ხელით შეყვანილ) ადგილს სტატუსი არ აქვს
        _status = place.UrlNavigation is null ? null : $"{place.UrlNavigation.State} | {place.UrlNavigation.Url}";
    }

    protected override string? GetStatus()
    {
        return _status;
    }

    //ჩანაწერის მენიუ VisitSubMenuCommand-ის ყაიდაზე: წაშლა, ველების თანმიმდევრობით რედაქტირება და
    //თითო ველი მიმდინარე მნიშვნელობით (მისამართი უცვლელი პუნქტით); გასაღები პუნქტის სახელია
    public override CliMenuSet GetSubMenu()
    {
        var placeSubMenuSet = new CliMenuSet($"Place => {Name}");
        placeSubMenuSet.AddMenuItem(new DeleteCruderRecordCliMenuCommand(_placeCruder, Name));
        placeSubMenuSet.AddMenuItem(new EditItemAllFieldsInSequenceCliMenuCommand(_placeCruder, Name));
        _placeCruder.FillDetailsSubMenu(placeSubMenuSet, Name);
        placeSubMenuSet.AddEscapeCommand("Exit to Places menu");
        return placeSubMenuSet;
    }
}
