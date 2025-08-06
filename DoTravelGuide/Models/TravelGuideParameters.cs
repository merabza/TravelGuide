//Created by ProjectParametersClassCreator at 7/24/2025 11:44:10 PM

using System.Collections.Generic;
using LibApiClientParameters;
using LibDatabaseParameters;
using LibFileParameters.Interfaces;
using LibFileParameters.Models;

namespace DoTravelGuide.Models;

public sealed class TravelGuideParameters : IParametersWithApiClients, IParametersWithDatabaseServerConnections,
    IParametersWithSmartSchemas
{
    public string? LogFolder { get; set; }
    //public string? DatabaseConnectionName { get; set; }

    public DatabaseParameters? DatabaseParameters { get; init; }

    public Dictionary<string, TaskModel> Tasks { get; set; } = [];
    public Dictionary<string, ApiClientSettings> ApiClients { get; } = [];

    public bool CheckBeforeSave()
    {
        return true;
    }

    public Dictionary<string, DatabaseServerConnectionData> DatabaseServerConnections { get; init; } = [];

    public Dictionary<string, SmartSchema> SmartSchemas { get; } = [];

    public TaskModel? GetTask(string taskName)
    {
        return Tasks.GetValueOrDefault(taskName);
    }

    public bool CheckNewTaskNameValid(string oldTaskName, string newTaskName)
    {
        if (oldTaskName == newTaskName) return true;

        if (!Tasks.ContainsKey(oldTaskName)) return false;

        return !Tasks.ContainsKey(newTaskName);
    }

    public bool RemoveTask(string taskName)
    {
        return Tasks.Remove(taskName);
    }

    public bool AddTask(string newTaskName, TaskModel task)
    {
        return Tasks.TryAdd(newTaskName, task);
    }
}