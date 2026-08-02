using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using SystemTools.SystemToolsShared;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.MenuCommands;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class DeleteTaskCommand : CliMenuCommand
{
    private readonly string _taskName;
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public DeleteTaskCommand(ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory,
        string taskName) : base("Delete Task", EMenuAction.LevelUp)
    {
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
        _taskName = taskName;
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

        //წაშლის დადასტურება
        if (!Inputer.InputBool($"This will Delete Task {_taskName}. are you sure?", false, false))
        {
            return ValueTask.FromResult(false);
        }

        //ამოცანის წაშლა საწყის წერტილებთან ერთად და ბაზაში შენახვა
        repository.DeleteTask(task);
        repository.SaveChanges();

        return ValueTask.FromResult(true);
    }
}
