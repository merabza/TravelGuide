using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using SystemTools.SystemToolsShared;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.MenuCommands;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class DeleteStartPointCommand : CliMenuCommand
{
    private readonly string _startPoint;
    private readonly string _taskName;
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public DeleteStartPointCommand(ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory,
        string taskName, string startPoint) : base("Delete Start Point", EMenuAction.LevelUp)
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

        //წაშლის დადასტურება
        if (!Inputer.InputBool($"This will Delete Start Point {_startPoint}. are you sure?", false, false))
        {
            return ValueTask.FromResult(false);
        }

        //საწყისი წერტილის წაშლა და ბაზაში შენახვა
        repository.DeleteStartPoint(startPoint);
        repository.SaveChanges();

        return ValueTask.FromResult(true);
    }
}
