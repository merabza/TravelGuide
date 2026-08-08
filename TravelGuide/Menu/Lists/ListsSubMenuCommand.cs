using System;
using AppCliTools.CliMenu;
using AppCliTools.CliParameters.CliMenuCommands;
using SystemTools.SystemToolsShared;
using TravelGuideRepoInterfaces;

namespace TravelGuide.Menu.Lists;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class ListsSubMenuCommand : CliMenuCommand
{
    private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public ListsSubMenuCommand(ITravelGuideRepositoryCreatorFactory travelGuideRepositoryCreatorFactory) : base("Lists",
        EMenuAction.LoadSubMenu)
    {
        _travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
    }

    public override CliMenuSet GetSubMenu()
    {
        //სიების რედაქტორების ქვემენიუს აგება
        var listsSubMenuSet = new CliMenuSet("Lists");

        try
        {
            ITravelGuideRepository repository = _travelGuideRepositoryCreatorFactory.GetTravelGuideRepository();
            //მოტოციკლების სიის რედაქტორი
            listsSubMenuSet.AddMenuItem(new CruderListCliMenuCommand(new MotorcycleCruder(repository)));
        }
        catch (Exception e)
        {
            //ბაზასთან დაკავშირება ვერ მოხერხდა — მენიუ რედაქტორების გარეშე აეწყობა
            StShared.WriteException(e, true);
        }

        listsSubMenuSet.AddEscapeCommand("Exit to Main menu");
        return listsSubMenuSet;
    }
}
