using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;

namespace TravelGuide.Menu.Visits;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class VisitSubMenuCommand : CliMenuCommand
{
    private readonly VisitCruder _visitCruder;
    private readonly int _visitId;

    //წარწერა (თარიღი და მოტოციკლი) მშობელი მენიუს აგებისას დგება, რომ აქ ბაზასთან მიმართვა აღარ დასჭირდეს
    public VisitSubMenuCommand(VisitCruder visitCruder, int visitId, string caption) : base(caption,
        EMenuAction.LoadSubMenu)
    {
        _visitCruder = visitCruder;
        _visitId = visitId;
    }

    //ვიზიტის მენიუ My Places-ის ჩანაწერის მენიუს იმეორებს: წაშლა, ველების თანმიმდევრობით რედაქტირება
    //და თითო ველი მიმდინარე მნიშვნელობით. წარწერა-გასაღები რედაქტირებისას იცვლება, ამიტომ ყოველ
    //გადაწყობაზე ვიზიტის იდენტიფიკატორით თავიდან დგინდება (Name ძველი რჩება, სანამ ზედა მენიუ განახლდება)
    public override CliMenuSet GetSubMenu()
    {
        string recordKey = _visitCruder.GetRecordKeyByVisitId(_visitId) ?? Name;
        var visitSubMenuSet = new CliMenuSet($"Visit => {recordKey}");
        visitSubMenuSet.AddMenuItem(new DeleteCruderRecordCliMenuCommand(_visitCruder, recordKey));
        visitSubMenuSet.AddMenuItem(new EditItemAllFieldsInSequenceCliMenuCommand(_visitCruder, recordKey));
        _visitCruder.FillDetailsSubMenu(visitSubMenuSet, recordKey);
        visitSubMenuSet.AddEscapeCommand("Exit to Place menu");
        return visitSubMenuSet;
    }
}
