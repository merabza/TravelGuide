//Created by DeleteTaskCommandCreator at 7/24/2025 11:44:10 PM

using CliMenu;
using DoTravelGuide.Models;
using LibDataInput;
using LibParameters;
using SystemToolsShared;

namespace TravelGuide.MenuCommands;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class DeleteTaskCommand : CliMenuCommand
{
    private readonly ParametersManager _parametersManager;
    private readonly string _taskName;

    public DeleteTaskCommand(ParametersManager parametersManager, string taskName) : base("Delete Task",
        EMenuAction.LevelUp)
    {
        _parametersManager = parametersManager;
        _taskName = taskName;
    }

    protected override bool RunBody()
    {
        var parameters = (TravelGuideParameters)_parametersManager.Parameters;
        var task = parameters.GetTask(_taskName);
        if (task == null)
        {
            StShared.WriteErrorLine($"Task {_taskName} does not found", true);
            return false;
        }

        if (!Inputer.InputBool($"This will Delete  Task {_taskName}.are you sure ? ", false, false)) return false;

        parameters.RemoveTask(_taskName);
        _parametersManager.Save(parameters, $"Task {_taskName} deleted.");
        return true;
    }
}