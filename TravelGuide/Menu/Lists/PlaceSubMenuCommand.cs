using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using TravelGuideDbModels;

namespace TravelGuide.Menu.Lists;

//ადგილის ჩანაწერის მენიუს პუნქტი: სახელი დასახელებაა (მშობელი სიის აგებისას დადგენილი წარწერა),
//სტატუსში ჩანაწერის სტატუსი და მისამართი ჩანს, რედაქტორის გასაღები კი მისამართია
public sealed class PlaceSubMenuCommand : CliMenuCommand
{
    private readonly PlaceCruder _placeCruder;
    private readonly string _recordKey;
    private readonly string _status;

    public PlaceSubMenuCommand(PlaceCruder placeCruder, PlaceModel place, string caption) : base(caption,
        EMenuAction.LoadSubMenu)
    {
        _placeCruder = placeCruder;
        _recordKey = place.Url;
        _status = $"{place.State} | {place.Url}";
    }

    protected override string GetStatus()
    {
        return _status;
    }

    //ჩანაწერის მენიუ VisitSubMenuCommand-ის ყაიდაზე: წაშლა, ველების თანმიმდევრობით რედაქტირება და
    //თითო ველი მიმდინარე მნიშვნელობით (მისამართი უცვლელი პუნქტით)
    public override CliMenuSet GetSubMenu()
    {
        var placeSubMenuSet = new CliMenuSet($"Place => {Name}");
        placeSubMenuSet.AddMenuItem(new DeleteCruderRecordCliMenuCommand(_placeCruder, _recordKey));
        placeSubMenuSet.AddMenuItem(new EditItemAllFieldsInSequenceCliMenuCommand(_placeCruder, _recordKey));
        _placeCruder.FillDetailsSubMenu(placeSubMenuSet, _recordKey);
        placeSubMenuSet.AddEscapeCommand("Exit to Places menu");
        return placeSubMenuSet;
    }
}
