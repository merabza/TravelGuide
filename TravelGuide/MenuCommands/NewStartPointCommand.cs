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
public sealed class NewStartPointCommand : CliMenuCommand
{
    private readonly string _taskName;
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public NewStartPointCommand(ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory,
        string taskName) : base("New Start Point", EMenuAction.Reload)
    {
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
        _taskName = taskName;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        MenuAction = EMenuAction.Reload;

        //საწყისი წერტილის დამატების პროცესი დაიწყო
        Console.WriteLine("Create new Start Point started");

        ITravelGuideRepository repository = _travelGuideRepositoryCreatorFactory.GetTravelGuideRepository();

        //მოვძებნოთ ამოცანა ბაზაში
        TaskModel? task = repository.GetTaskByName(_taskName);
        if (task is null)
        {
            StShared.WriteErrorLine($"Task with Name {_taskName} not found", true);
            return ValueTask.FromResult(false);
        }

        //ახალი საწყისი წერტილის შეყვანა
        string? startPoint = Inputer.InputText("New Start Point", null);
        if (string.IsNullOrWhiteSpace(startPoint))
        {
            return ValueTask.FromResult(false);
        }

        //შევამოწმოთ ხომ არ არსებობს ამ ამოცანას იგივე საწყისი წერტილი
        if (repository.GetStartPoint(task.TaskId, startPoint) is not null)
        {
            StShared.WriteErrorLine($"Start Point {startPoint} already exists", true);
            return ValueTask.FromResult(false);
        }

        //ახალი საწყისი წერტილის დამატება და ბაზაში შენახვა
        repository.AddStartPoint(task.TaskId, startPoint);
        repository.SaveChanges();

        //საწყისი წერტილის დამატების პროცესი დასრულდა
        Console.WriteLine("Create New Start Point Finished");
        return ValueTask.FromResult(true);
    }
}
