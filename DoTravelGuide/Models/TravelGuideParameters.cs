//Created by ProjectParametersClassCreator at 7/24/2025 11:44:10 PM

using System.Collections.Generic;
using ParametersManagement.LibApiClientParameters;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibFileParameters.Interfaces;
using ParametersManagement.LibFileParameters.Models;

namespace DoTravelGuide.Models;

public sealed class TravelGuideParameters : IParametersWithApiClients, IParametersWithDatabaseServerConnections,
    IParametersWithSmartSchemas
{
    public string? LogFolder { get; set; }
    //public string? DatabaseConnectionName { get; set; }

    public DatabaseParameters? DatabaseParameters { get; init; }

    public Dictionary<string, ApiClientSettings> ApiClients { get; } = [];

    public bool CheckBeforeSave()
    {
        return true;
    }

    public Dictionary<string, DatabaseServerConnectionData> DatabaseServerConnections { get; init; } = [];

    public Dictionary<string, SmartSchema> SmartSchemas { get; } = [];

}
