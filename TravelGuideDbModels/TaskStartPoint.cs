namespace TravelGuideDbModels;

public sealed class TaskStartPoint
{
    public int TspId { get; init; }
    public int TaskId { get; init; }
    public required string StartPoint { get; init; }

    public TaskModel TaskNavigation
    {
        get => field ?? throw new InvalidOperationException("Uninitialized property: " + nameof(TaskNavigation));
        init;
    }
}
