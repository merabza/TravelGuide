//Created by ProjectParametersClassCreator at 7/24/2025 11:44:10 PM

using LibParameters;
using System.Collections.Generic;
using LibDatabaseParameters;

namespace DoTravelGuide.Models;

public sealed class TravelGuideParameters : IParameters
{
    public string? LogFolder { get; set; }
    public string? DatabaseConnectionName { get; set; }
    public Dictionary<string, DatabaseServerConnectionData> DatabaseServerConnections { get; set; } = [];
    public DatabaseParameters? DatabaseParameters { get; set; }
    public Dictionary<string, TaskModel> Tasks { get; set; } = [];

    public bool CheckBeforeSave()
    {
        return true;
    }

    public TaskModel? GetTask(string taskName)
    {
        return Tasks.GetValueOrDefault(taskName);
    }

    public bool CheckNewTaskNameValid(string oldTaskName, string newTaskName)
    {
        if (oldTaskName == newTaskName)
        {
            return true;
        }

        if (!Tasks.ContainsKey(oldTaskName))
        {
            return false;
        }

        return !Tasks.ContainsKey(newTaskName);
    }

    public bool RemoveTask(string taskName)
    {
        if (!Tasks.ContainsKey(taskName))
        {
            return false;
        }

        Tasks.Remove(taskName);
        return true;
    }

    public bool AddTask(string newTaskName, TaskModel task)
    {
        return Tasks.TryAdd(newTaskName, task);
    }
}