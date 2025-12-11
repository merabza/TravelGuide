//Created by TaskCommandCreator at 7/24/2025 11:44:10 PM

using System;
using System.Diagnostics;
using CliMenu;
using DoTravelGuide.Models;
using LibParameters;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

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

    protected override bool RunBody()
    {
        MenuAction = EMenuAction.Reload;
        var parameters = (TravelGuideParameters)_parametersManager.Parameters;
        var task = parameters.GetTask(_taskName);
        if (task == null)
        {
            StShared.WriteErrorLine($"Task {_taskName} does not found", true);
            return false;
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
        return true;
    }
}