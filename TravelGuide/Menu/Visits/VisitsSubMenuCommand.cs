using AppCliTools.CliMenu;

namespace TravelGuide.Menu.Visits;

public sealed class VisitsSubMenuCommand : CliMenuCommand
{
    public VisitsSubMenuCommand() : base("Visits", EMenuAction.LoadSubMenu)
    {
    }

    public override CliMenuSet GetSubMenu()
    {
        //ვიზიტების ქვემენიუს აგება
        var visitsSubMenuSet = new CliMenuSet("Visits");

        //ახალი ვიზიტის შეყვანა
        visitsSubMenuSet.AddMenuItem(new NewVisitCommand());
        //ბოლო ვიზიტების სია
        visitsSubMenuSet.AddMenuItem(new LastVisitsCommand());
        //რეკომენდებული ვიზიტების სია
        visitsSubMenuSet.AddMenuItem(new RecommendedVisitsCommand());

        visitsSubMenuSet.AddEscapeCommand("Exit to Main menu");
        return visitsSubMenuSet;
    }
}
