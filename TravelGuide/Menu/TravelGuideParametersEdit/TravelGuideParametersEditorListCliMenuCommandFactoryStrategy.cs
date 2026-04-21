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
    private readonly IApplication _app;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TravelGuideParametersEditorListCliMenuCommandFactoryStrategy> _logger;
    private readonly IParametersManager _parametersManager;

    // ReSharper disable once ConvertToPrimaryConstructor
    public TravelGuideParametersEditorListCliMenuCommandFactoryStrategy(IApplication app,
        ILogger<TravelGuideParametersEditorListCliMenuCommandFactoryStrategy> logger,
        IHttpClientFactory httpClientFactory, IParametersManager parametersManager)
    {
        _app = app;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
    }

    //public string StrategyName => nameof(TravelGuideParametersEditorListCliMenuCommandFactoryStrategy);

    public CliMenuCommand CreateMenuCommand()
    {
        var parameters = (TravelGuideParameters)_parametersManager.Parameters;

        var travelGuideParametersEditor =
            new TravelGuideParametersEditor(_app, _logger, _httpClientFactory, parameters, _parametersManager);
        return new ParametersEditorListCliMenuCommand(travelGuideParametersEditor);
    }
}
