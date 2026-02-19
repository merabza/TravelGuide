//Created by TaskSubMenuCommandCreator at 7/24/2025 11:44:10 PM

using System;
using System.Globalization;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using AppCliTools.LibDataInput;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;

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
        string key = ConsoleKey.Escape.Value().ToLower(CultureInfo.CurrentCulture);
        taskSubMenuSet.AddMenuItem(key, new ExitToMainMenuCliMenuCommand("Exit to level up menu", null), key.Length);
        return taskSubMenuSet;
    }
}
