using System;
using System.Collections.Generic;
using System.Linq;
using AppCliTools.CliMenu;
using SystemTools.SystemToolsShared;
using TravelGuideRepoInterfaces;

namespace TravelGuide.MenuCommands;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class TasksListFactoryStrategy : IMenuCommandListFactoryStrategy
{
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public TasksListFactoryStrategy(ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory)
    {
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
    }

    public List<CliMenuCommand> CreateMenuCommandsList()
    {
        try
        {
            //ამოცანების ჩამონათვალის წამოღება ბაზიდან
            ITravelGuideRepository repository = _travelGuideRepositoryCreatorFactory.GetTravelGuideRepository();
            return
            [
                .. repository.GetTasksList().OrderBy(o => o.TaskName, StringComparer.Ordinal).Select(task =>
                    new TaskSubMenuCommand(_travelGuideRepositoryCreatorFactory, task.TaskName))
            ];
        }
        catch (Exception e)
        {
            //ბაზასთან დაკავშირება ვერ მოხერხდა — მენიუ ამოცანების გარეშე აეწყობა
            StShared.WriteException(e, true);
            return [];
        }
    }
}
