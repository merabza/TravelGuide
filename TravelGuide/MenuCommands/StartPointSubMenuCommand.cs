using AppCliTools.CliMenu;
using TravelGuideRepoInterfaces;

namespace TravelGuide.MenuCommands;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class StartPointSubMenuCommand : CliMenuCommand
{
    private readonly string _taskName;
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public StartPointSubMenuCommand(ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory,
        string taskName, string startPoint) : base(startPoint, EMenuAction.LoadSubMenu)
    {
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
        _taskName = taskName;
    }

    public override CliMenuSet GetSubMenu()
    {
        //საწყისი წერტილის ქვემენიუს აგება
        var startPointSubMenuSet = new CliMenuSet($" Task => {_taskName},  Start Point => {Name}");
        startPointSubMenuSet.AddMenuItem(
            new DeleteStartPointCommand(_travelGuideRepositoryCreatorFactory, _taskName, Name));
        startPointSubMenuSet.AddMenuItem(
            new EditStartPointCommand(_travelGuideRepositoryCreatorFactory, _taskName, Name));
        startPointSubMenuSet.AddEscapeCommand();
        return startPointSubMenuSet;
    }
}
