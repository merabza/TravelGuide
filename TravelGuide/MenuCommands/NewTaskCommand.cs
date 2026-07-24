using System;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using SystemTools.SystemToolsShared;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.MenuCommands;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class NewTaskCommand : CliMenuCommand
{
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public NewTaskCommand(ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory) : base("New Task",
        EMenuAction.Reload)
    {
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        MenuAction = EMenuAction.Reload;

        //ამოცანის შექმნის პროცესი დაიწყო
        Console.WriteLine("Create new Task started");

        //ახალი ამოცანის სახელის შეყვანა
        string? newTaskName = Inputer.InputText("New Task Name", null);
        if (string.IsNullOrEmpty(newTaskName))
        {
            return ValueTask.FromResult(false);
        }

        ITravelGuideRepository repository = _travelGuideRepositoryCreatorFactory.GetTravelGuideRepository();

        //შევამოწმოთ ხომ არ არსებობს ბაზაში ამოცანა იგივე სახელით
        if (repository.GetTaskByName(newTaskName) is not null)
        {
            StShared.WriteErrorLine($"Task with Name {newTaskName} already exists", true);
            return ValueTask.FromResult(false);
        }

        //ამოცანის საწყისი წერტილის შეყვანა
        string? startPoint = Inputer.InputText("Start Point", null);
        if (string.IsNullOrEmpty(startPoint))
        {
            return ValueTask.FromResult(false);
        }

        //ახალი ამოცანის შექმნა საწყისი წერტილით და ბაზაში შენახვა
        repository.CreateTask(new TaskModel
        {
            TaskName = newTaskName, StartPoints = { new TaskStartPoint { StartPoint = startPoint } }
        });
        repository.SaveChanges();

        //ამოცანის შექმნის პროცესი დასრულდა
        Console.WriteLine("Create New Task Finished");
        return ValueTask.FromResult(true);
    }
}
