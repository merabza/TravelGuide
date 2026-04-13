using System.Collections.Generic;
using TravelGuide.Menu.TravelGuideParametersEdit;

namespace TravelGuide.Menu;

public static class MenuData
{
    public static List<string> MenuCommandNames { get; } =
    [
        //ძირითადი პარამეტრების რედაქტირება
        TravelGuideParametersEditor.MenuCommandName,
        ////სერვერის პარამეტრების რედაქტირება
        //SupportToolsServerEditorCliMenuCommand.MenuCommandName,
        ////ახალი პროექტების შემქმნელი სუბმენიუ
        //ProjectCreatorSubMenuCliMenuCommand.MenuCommandName,
        ////ახალი პროექტის შექმნა 
        //$"New {ProjectCruder.MenuCommandName}",
        ////პროექტის დაიმპორტება
        //ImportProjectCliMenuCommand.MenuCommandName,
        ////ყველა პროექტის git-ის სინქრონიზაცია V2
        //SyncAllProjectsAllGitsCliMenuCommandV2.MenuCommandName,
        ////ყველა პროექტის პაკეტების განახლება
        //UpdateOutdatedPackagesCliMenuCommand.MenuCommandName,
        ////ყველა ჯგუფების, ყველა სოლუშენის, ყველა პროექტის გასუფთავება
        //ClearAllGroupsAllSolutionsAllProjectsCliMenuCommand.MenuCommandName,
        ////პროექტების ჯგუფების ჩამონათვალი
        //ProjectGroupSubMenuCliMenuCommand.MenuCommandListName,
        ////ბოლოს გამოყენებული ბრძანებების ჩამონათვალი
        //nameof(RecentCommandCliMenuCommand)
    ];
}
