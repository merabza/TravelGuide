//Created by EditTaskNameCommandCreator at 7/24/2025 11:44:10 PM

using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using DoTravelGuide.Models;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;

namespace TravelGuide.MenuCommands;

public sealed class EditTaskNameCommand : CliMenuCommand
{
    private readonly ParametersManager _parametersManager;
    private readonly string _taskName;

    // ReSharper disable once ConvertToPrimaryConstructor
    public EditTaskNameCommand(ParametersManager parametersManager, string taskName) : base("Edit Task",
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

        //ამოცანის სახელის რედაქტირება
        string? newTaskName = Inputer.InputText("change  Task Name ", _taskName);
        if (string.IsNullOrWhiteSpace(newTaskName) || _taskName == newTaskName)
        {
            return ValueTask.FromResult(false);
        }

        if (!parameters.RemoveTask(_taskName))
        {
            StShared.WriteErrorLine(
                $"Cannot change  Task with name {_taskName} to {newTaskName}, because cannot remove this  task", true);
            return ValueTask.FromResult(false);
        }

        if (!parameters.AddTask(newTaskName, task))
        {
            StShared.WriteErrorLine(
                $"Cannot change  Task with name {_taskName} to {newTaskName}, because cannot add this  task", true);
            return ValueTask.FromResult(false);
        }

        _parametersManager.Save(parameters, $" Task Renamed from {_taskName} To {newTaskName}");

        return ValueTask.FromResult(true);
    }

    protected override string GetStatus()
    {
        return _taskName;
    }
}
