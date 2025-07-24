//Created by ProjectParametersEditorClassCreator at 7/24/2025 11:44:10 PM

using System.Net.Http;
using CliParameters;
using CliParameters.FieldEditors;
using LibParameters;
using Microsoft.Extensions.Logging;
using DoTravelGuide.Models;
using CliParametersDataEdit.FieldEditors;

namespace TravelGuide;

public sealed class TravelGuideParametersEditor : ParametersEditor
{
    public TravelGuideParametersEditor(IParameters parameters, ParametersManager parametersManager, ILogger logger,
        IHttpClientFactory httpClientFactory) : base("TravelGuide Parameters Editor", parameters, parametersManager)
    {
        FieldEditors.Add(new FolderPathFieldEditor(nameof(TravelGuideParameters.LogFolder)));
        FieldEditors.Add(new DatabaseServerConnectionNameFieldEditor(logger, httpClientFactory,
            nameof(TravelGuideParameters.DatabaseConnectionName), parametersManager, true));
    }
}