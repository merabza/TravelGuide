using AppCliTools.CliMenu;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Lists;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class ListsSubMenuCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public ListsSubMenuCommandFactoryStrategy(ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory)
    {
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
    }

    public CliMenuCommand CreateMenuCommand()
    {
        return new ListsSubMenuCommand(_travelGuideRepositoryCreatorFactory);
    }
}
