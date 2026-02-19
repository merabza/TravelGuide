//Created by TaskCommandCreator at 7/24/2025 11:44:10 PM

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using DoTravelGuide.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;

namespace TravelGuide.MenuCommands;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class TaskCommand : CliMenuCommand
{
    private readonly ILogger _logger;
    private readonly IParametersManager _parametersManager;
    private readonly string _taskName;

    public TaskCommand(ILogger logger, IParametersManager parametersManager, string taskName) : base("Task Command")
    {
        _logger = logger;
        _parametersManager = parametersManager;
        _taskName = taskName;
    }

    protected override ValueTask<bool> RunBody(CancellationToken cancellationToken = default)
    {
        MenuAction = EMenuAction.Reload;
        var parameters = (TravelGuideParameters)_parametersManager.Parameters;
        TaskModel? task = parameters.GetTask(_taskName);
        if (task == null)
        {
            StShared.WriteErrorLine($"Task {_taskName} does not found", true);
            return ValueTask.FromResult(false);
        }

        var crawlerRunner = new TravelGuideTaskRunner(_logger, parameters, _taskName, task);

        //დავინიშნოთ დრო
        var watch = Stopwatch.StartNew();
        Console.WriteLine("Crawler is running...");
        Console.WriteLine("-- - ");
        crawlerRunner.Run();
        watch.Stop();
        Console.WriteLine("-- - ");
        Console.WriteLine($"Crawler Finished.Time taken: {watch.Elapsed.Seconds} second(s)");
        StShared.Pause();
        return ValueTask.FromResult(true);
    }
}
