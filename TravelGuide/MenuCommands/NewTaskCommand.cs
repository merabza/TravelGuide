//Created by NewTaskCommandCreator at 7/24/2025 11:44:10 PM

using System;
using CliMenu;
using DoTravelGuide.Models;
using LibDataInput;
using LibParameters;
using SystemToolsShared;

namespace TravelGuide.MenuCommands;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class NewTaskCommand : CliMenuCommand
{
    private readonly ParametersManager _parametersManager;

    public NewTaskCommand(ParametersManager parametersManager) : base("New Task")
    {
        _parametersManager = parametersManager;
    }

    protected override bool RunBody()
    {
        MenuAction = EMenuAction.Reload;
        var parameters = (TravelGuideParameters)_parametersManager.Parameters;

        //ამოცანის შექმნის პროცესი დაიწყო
        Console.WriteLine("Create new Task started");

        var newTaskName = Inputer.InputText("New Task Name", null);
        if (string.IsNullOrEmpty(newTaskName)) return false;

        //ახალი ამოცანის შექმნა და ჩამატება ამოცანების სიაში
        if (!parameters.AddTask(newTaskName, new TaskModel()))
        {
            StShared.WriteErrorLine($"Task with Name {newTaskName} does not created", true);
            return false;
        }

        //პარამეტრების შენახვა (ცვლილებების გათვალისწინებით)
        _parametersManager.Save(parameters, "Create New Task Finished");
        return true;
    }
}