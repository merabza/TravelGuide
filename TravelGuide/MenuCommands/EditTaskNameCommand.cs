//Created by EditTaskNameCommandCreator at 7/24/2025 11:44:10 PM

using CliMenu;
using DoTravelGuide.Models;
using LibDataInput;
using LibParameters;
using SystemToolsShared;

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

    protected override bool RunBody()
    {
        var parameters = (TravelGuideParameters)_parametersManager.Parameters;
        var task = parameters.GetTask(_taskName);
        if (task == null)
        {
            StShared.WriteErrorLine($"Task {_taskName} does not found", true);
            return false;
        }

        //ამოცანის სახელის რედაქტირება
        var newTaskName = Inputer.InputText("change  Task Name ", _taskName);
        if (string.IsNullOrWhiteSpace(newTaskName)) return false;

        if (_taskName == newTaskName) return false;

        if (!parameters.RemoveTask(_taskName))
        {
            StShared.WriteErrorLine(
                $"Cannot change  Task with name {_taskName} to {newTaskName}, because cannot remove this  task", true);
            return false;
        }

        if (!parameters.AddTask(newTaskName, task))
        {
            StShared.WriteErrorLine(
                $"Cannot change  Task with name {_taskName} to {newTaskName}, because cannot add this  task", true);
            return false;
        }

        _parametersManager.Save(parameters, $" Task Renamed from {_taskName} To {newTaskName}");

        return true;
    }

    protected override string GetStatus()
    {
        return _taskName;
    }
}