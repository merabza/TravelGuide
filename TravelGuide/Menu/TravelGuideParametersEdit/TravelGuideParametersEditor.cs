using System.Net.Http;
using AppCliTools.CliParameters;
using AppCliTools.CliParameters.FieldEditors;
using AppCliTools.CliParametersDataEdit.Cruders;
using AppCliTools.CliParametersDataEdit.FieldEditors;
using AppCliTools.CliParametersEdit.Cruders;
using DoTravelGuide.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibFileParameters.Models;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;

namespace TravelGuide.Menu.TravelGuideParametersEdit;

public sealed class TravelGuideParametersEditor : ParametersEditor
{
    public TravelGuideParametersEditor(IApplication app, ILogger logger, IHttpClientFactory httpClientFactory,
        IParameters parameters, IParametersManager parametersManager) : base("Travel Guide Parameters Editor",
        parameters, parametersManager)
    {
        FieldEditors.Add(new FolderPathFieldEditor(nameof(TravelGuideParameters.LogFolder)));

        FieldEditors.Add(new DictionaryFieldEditor<DatabaseServerConnectionCruder, DatabaseServerConnectionData>(
            nameof(TravelGuideParameters.DatabaseServerConnections),
            x => new DatabaseServerConnectionCruder(app, logger, httpClientFactory, parametersManager, x)));

        FieldEditors.Add(new DatabaseParametersFieldEditor(app, logger, httpClientFactory,
            nameof(TravelGuideParameters.DatabaseParameters), parametersManager));

        FieldEditors.Add(new DictionaryFieldEditor<SmartSchemaCruder, SmartSchema>(
            nameof(TravelGuideParameters.SmartSchemas), x => new SmartSchemaCruder(parametersManager, x)));
    }
}
