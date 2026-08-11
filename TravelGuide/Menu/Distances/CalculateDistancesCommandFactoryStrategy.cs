using System.Net.Http;
using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Distances;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class CalculateDistancesCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IParametersManager _parametersManager;
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public CalculateDistancesCommandFactoryStrategy(IParametersManager parametersManager,
        ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory, IHttpClientFactory httpClientFactory)
    {
        _parametersManager = parametersManager;
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
        _httpClientFactory = httpClientFactory;
    }

    public CliMenuCommand CreateMenuCommand()
    {
        return new CalculateDistancesCommand(_parametersManager, _travelGuideRepositoryCreatorFactory,
            _httpClientFactory);
    }
}
