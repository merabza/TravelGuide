using System.Net.Http;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using DoTravelGuide.Models;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;

namespace TravelGuide.Menu.TravelGuideParametersEdit;

// ReSharper disable once UnusedType.Global
public class TravelGuideParametersEditorListCliMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IApplication _app;
    private readonly ILogger<TravelGuideParametersEditorListCliMenuCommandFactoryStrategy> _logger;

    // ReSharper disable once ConvertToPrimaryConstructor
    public TravelGuideParametersEditorListCliMenuCommandFactoryStrategy(IApplication app,
        ILogger<TravelGuideParametersEditorListCliMenuCommandFactoryStrategy> logger,
        IHttpClientFactory httpClientFactory)
    {
        _app = app;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string MenuCommandName => TravelGuideParametersEditor.MenuCommandName;

    public CliMenuCommand CreateMenuCommand(IParametersManager parametersManager)
    {
        var parameters = (TravelGuideParameters)parametersManager.Parameters;

        var travelGuideParametersEditor =
            new TravelGuideParametersEditor(_app, _logger, _httpClientFactory, parameters, parametersManager);
        return new ParametersEditorListCliMenuCommand(travelGuideParametersEditor);
    }
}
