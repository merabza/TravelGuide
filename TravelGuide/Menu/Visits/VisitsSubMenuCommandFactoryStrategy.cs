using AppCliTools.CliMenu;

namespace TravelGuide.Menu.Visits;

public sealed class VisitsSubMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    public CliMenuCommand CreateMenuCommand()
    {
        return new VisitsSubMenuCommand();
    }
}
