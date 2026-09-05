using System.Net.Http;
using AppCliTools.CliMenu;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Lists;


public sealed class ListsSubMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public ListsSubMenuCommandFactoryStrategy(ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory,
        IHttpClientFactory httpClientFactory)
    {
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
        _httpClientFactory = httpClientFactory;
    }

    public CliMenuCommand CreateMenuCommand()
    {
        return new ListsSubMenuCommand(_travelGuideRepositoryCreatorFactory, _httpClientFactory);
    }
}
