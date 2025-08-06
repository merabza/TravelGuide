//Created by ProjectParametersEditorClassCreator at 7/24/2025 11:44:10 PM

using System.Net.Http;
using CliParameters;
using CliParameters.FieldEditors;
using CliParametersDataEdit.Cruders;
using CliParametersDataEdit.FieldEditors;
using CliParametersEdit.Cruders;
using DoTravelGuide.Models;
using LibDatabaseParameters;
using LibFileParameters.Models;
using LibParameters;
using Microsoft.Extensions.Logging;

namespace TravelGuide;

public sealed class TravelGuideParametersEditor : ParametersEditor
{
    public TravelGuideParametersEditor(IParameters parameters, ParametersManager parametersManager, ILogger logger,
        IHttpClientFactory httpClientFactory) : base("TravelGuide Parameters Editor", parameters, parametersManager)
    {
        FieldEditors.Add(new FolderPathFieldEditor(nameof(TravelGuideParameters.LogFolder)));

        //FieldEditors.Add(new DatabaseServerConnectionNameFieldEditor(logger, httpClientFactory,
        //    nameof(TravelGuideParameters.DatabaseConnectionName), parametersManager, true));

        FieldEditors.Add(new DictionaryFieldEditor<DatabaseServerConnectionCruder, DatabaseServerConnectionData>(
            nameof(TravelGuideParameters.DatabaseServerConnections), logger, httpClientFactory, parametersManager));

        FieldEditors.Add(new DatabaseParametersFieldEditor(logger, httpClientFactory,
            nameof(TravelGuideParameters.DatabaseParameters), parametersManager));

        FieldEditors.Add(
            new DictionaryFieldEditor<SmartSchemaCruder, SmartSchema>(nameof(TravelGuideParameters.SmartSchemas),
                parametersManager));

    }
}