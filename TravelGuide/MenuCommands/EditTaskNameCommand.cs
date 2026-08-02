using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.LibDataInput;
using SystemTools.SystemToolsShared;
using TravelGuideDbModels;
using TravelGuideRepoInterfaces;

namespace TravelGuide.MenuCommands;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class EditTaskNameCommand : CliMenuCommand
{
    private readonly string _taskName;
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public EditTaskNameCommand(ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory,
        string taskName) : base("Edit Task Name", EMenuAction.LevelUp, EMenuAction.Reload, taskName)
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

        //ამოცანის სახელის რედაქტირება
        string? newTaskName = Inputer.InputText("change Task Name", _taskName);
        if (string.IsNullOrWhiteSpace(newTaskName) || _taskName == newTaskName)
        {
            return ValueTask.FromResult(false);
        }

        //შევამოწმოთ ხომ არ არსებობს ბაზაში ამოცანა იგივე სახელით
        if (repository.GetTaskByName(newTaskName) is not null)
        {
            StShared.WriteErrorLine($"Task with Name {newTaskName} already exists", true);
            return ValueTask.FromResult(false);
        }

        //ამოცანის სახელის შეცვლა და ბაზაში შენახვა
        task.TaskName = newTaskName;
        repository.UpdateTask(task);
        repository.SaveChanges();

        return ValueTask.FromResult(true);
    }

    protected override string GetStatus()
    {
        return _taskName;
    }
}
