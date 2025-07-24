//Created by ProjectTaskRunnerCreator at 7/24/2025 11:44:10 PM

using System;
using DoTravelGuide.Models;
using Microsoft.Extensions.Logging;
using SystemToolsShared;

namespace TravelGuide;

public sealed class TravelGuideTaskRunner
{
    private readonly ILogger _logger;
    private readonly TravelGuideParameters _par;
    private readonly string? _taskName;
    private readonly TaskModel? _task;

    public TravelGuideTaskRunner(ILogger logger, TravelGuideParameters par, string taskName, TaskModel task)
    {
        _logger = logger;
        _par = par;
        _taskName = taskName;
        _task = task;
    }

    public TravelGuideTaskRunner(ILogger logger, TravelGuideParameters par)
    {
        _logger = logger;
        _par = par;
        _taskName = null;
        _task = null;
    }

    public void Run()
    {
        try
        {
        }
        catch (Exception e)
        {
            StShared.WriteException(e, true);
            throw;
        }
    }
}