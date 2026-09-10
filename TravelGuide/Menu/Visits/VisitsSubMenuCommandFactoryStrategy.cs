using System.Net.Http;
using AppCliTools.CliMenu;
using ParametersManagement.LibParameters;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Visits;

public sealed class VisitsSubMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IParametersManager _parametersManager;
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public VisitsSubMenuCommandFactoryStrategy(IParametersManager parametersManager,
        ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory, IHttpClientFactory httpClientFactory)
    {
        _parametersManager = parametersManager;
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
        _httpClientFactory = httpClientFactory;
    }

    public CliMenuCommand CreateMenuCommand()
    {
        return new VisitsSubMenuCommand(_parametersManager, _travelGuideRepositoryCreatorFactory, _httpClientFactory);
    }
}
