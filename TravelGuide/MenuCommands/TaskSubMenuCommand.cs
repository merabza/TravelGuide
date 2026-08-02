using System;
using System.Linq;
using AppCliTools.CliMenu;
using SystemTools.SystemToolsShared;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.MenuCommands;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class TaskSubMenuCommand : CliMenuCommand
{
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public TaskSubMenuCommand(ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory,
        string taskName) : base(taskName, EMenuAction.LoadSubMenu)
    {
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
    }

    public override CliMenuSet GetSubMenu()
    {
        //ამოცანის ქვემენიუს აგება
        var taskSubMenuSet = new CliMenuSet($" Task => {Name}");
        taskSubMenuSet.AddMenuItem(new DeleteTaskCommand(_travelGuideRepositoryCreatorFactory, Name));
        taskSubMenuSet.AddMenuItem(new EditTaskNameCommand(_travelGuideRepositoryCreatorFactory, Name));
        taskSubMenuSet.AddMenuItem(new NewStartPointCommand(_travelGuideRepositoryCreatorFactory, Name));

        try
        {
            //ამოცანის საწყისი წერტილების ჩამონათვალი
            ITravelGuideRepository repository = _travelGuideRepositoryCreatorFactory.GetTravelGuideRepository();
            TaskModel? task = repository.GetTaskByName(Name);
            if (task is not null)
            {
                foreach (TaskStartPoint startPoint in task.StartPoints.OrderBy(o => o.StartPoint,
                             StringComparer.Ordinal))
                {
                    taskSubMenuSet.AddMenuItem(new StartPointSubMenuCommand(_travelGuideRepositoryCreatorFactory, Name,
                        startPoint.StartPoint));
                }
            }
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true);
        }

        taskSubMenuSet.AddEscapeCommand("Exit to Main menu");
        return taskSubMenuSet;
    }
}
