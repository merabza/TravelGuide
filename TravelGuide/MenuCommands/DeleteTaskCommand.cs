//Created by DeleteTaskCommandCreator at 7/24/2025 11:44:10 PM

using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using DoTravelGuide.Models;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;

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

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)

    {
        var parameters = (TravelGuideParameters)_parametersManager.Parameters;
        TaskModel? task = parameters.GetTask(_taskName);
        if (task == null)
        {
            StShared.WriteErrorLine($"Task {_taskName} does not found", true);
            return ValueTask.FromResult(false);
        }

        if (!Inputer.InputBool($"This will Delete  Task {_taskName}.are you sure ? ", false, false))
        {
            return ValueTask.FromResult(false);
        }

        parameters.RemoveTask(_taskName);
        _parametersManager.Save(parameters, $"Task {_taskName} deleted.");
        return ValueTask.FromResult(true);
    }
}
