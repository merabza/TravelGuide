using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using SystemTools.SystemToolsShared;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.MenuCommands;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class EditStartPointCommand : CliMenuCommand
{
    private readonly string _startPoint;
    private readonly string _taskName;
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public EditStartPointCommand(ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory,
        string taskName, string startPoint) : base("Edit Start Point", EMenuAction.LevelUp, EMenuAction.Reload,
        taskName)
    {
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
        _taskName = taskName;
        _startPoint = startPoint;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        MenuAction = EMenuAction.Reload;

        ITravelGuideRepository repository = _travelGuideRepositoryCreatorFactory.GetTravelGuideRepository();

        //მოვძებნოთ ამოცანა ბაზაში
        TaskModel? task = repository.GetTaskByName(_taskName);
        if (task is null)
        {
            StShared.WriteErrorLine($"Task with Name {_taskName} not found", true);
            return ValueTask.FromResult(false);
        }

        //მოვძებნოთ საწყისი წერტილი ბაზაში
        TaskStartPoint? startPoint = repository.GetStartPoint(task.TaskId, _startPoint);
        if (startPoint is null)
        {
            StShared.WriteErrorLine($"Start Point {_startPoint} not found", true);
            return ValueTask.FromResult(false);
        }

        //საწყისი წერტილის რედაქტირება
        string? newStartPoint = Inputer.InputText("change Start Point", _startPoint);
        if (string.IsNullOrWhiteSpace(newStartPoint) || _startPoint == newStartPoint)
        {
            return ValueTask.FromResult(false);
        }

        //შევამოწმოთ ხომ არ არსებობს ამ ამოცანას იგივე საწყისი წერტილი
        if (repository.GetStartPoint(task.TaskId, newStartPoint) is not null)
        {
            StShared.WriteErrorLine($"Start Point {newStartPoint} already exists", true);
            return ValueTask.FromResult(false);
        }

        //საწყისი წერტილის შეცვლა და ბაზაში შენახვა
        startPoint.StartPoint = newStartPoint;
        repository.UpdateStartPoint(startPoint);
        repository.SaveChanges();

        return ValueTask.FromResult(true);
    }
}
