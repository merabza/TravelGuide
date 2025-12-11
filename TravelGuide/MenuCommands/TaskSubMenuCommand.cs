//Created by TaskSubMenuCommandCreator at 7/24/2025 11:44:10 PM

using System;
using CliMenu;
using CliParameters.CliMenuCommands;
using LibDataInput;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace TravelGuide.MenuCommands;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class TaskSubMenuCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly ParametersManager _parametersManager;

    public TaskSubMenuCommand(ILogger logger, ParametersManager parametersManager, string taskName) : base(taskName,
        EMenuAction.LoadSubMenu)
    {
        _logger = logger;
        _parametersManager = parametersManager;
    }

    protected override bool RunBody()
    {
        return true;
    }

    public override CliMenuSet GetSubMenu()
    {
        var taskSubMenuSet = new CliMenuSet($" Task => {Name}");
        var deleteTaskCommand = new DeleteTaskCommand(_parametersManager, Name);
        taskSubMenuSet.AddMenuItem(deleteTaskCommand);
        taskSubMenuSet.AddMenuItem(new EditTaskNameCommand(_parametersManager, Name));
        taskSubMenuSet.AddMenuItem(new TaskCommand(_logger, _parametersManager, Name));
        //ეს საჭირო იქნება, თუ ამ მენიუში საჭირო გახდება ამოცანის დამატებითი რედაქტორების შექმნა
        //var parameters = (TravelGuideParameters)_parametersManager.Parameters;
        //var task = parameters.GetTask(Name);
        var key = ConsoleKey.Escape.Value().ToLower();
        taskSubMenuSet.AddMenuItem(key, new ExitToMainMenuCliMenuCommand("Exit to level up menu", null), key.Length);
        return taskSubMenuSet;
    }
}