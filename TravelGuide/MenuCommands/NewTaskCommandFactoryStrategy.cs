using AppCliTools.CliMenu;
using TravelGuideRepoInterfaces;

namespace TravelGuide.MenuCommands;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class NewTaskCommandFactoryStrategy : IMenuCommandFactoryStrategy
{
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public NewTaskCommandFactoryStrategy(ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory)
    {
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
    }

    public CliMenuCommand CreateMenuCommand()
    {
        return new NewTaskCommand(_travelGuideRepositoryCreatorFactory);
    }
}
