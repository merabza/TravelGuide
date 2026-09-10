using AppCliTools.CliMenu;
using TravelGuideRepoInterfaces;

namespace TravelGuide.MenuCommands;

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
