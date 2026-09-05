using System.Collections.Generic;
using TravelGuide.Menu.Lists;
using TravelGuide.Menu.TravelGuideParametersEdit;
using TravelGuide.Menu.Visits;
using TravelGuide.MenuCommands;

namespace TravelGuide.Menu;

public static class MenuData
{
    public static List<string> MenuCommandNames { get; } =
    [
        //ძირითადი პარამეტრების რედაქტირება
        nameof(TravelGuideParametersEditorListCliMenuCommandFactoryStrategy),
        //სიების რედაქტორების ქვემენიუ
        nameof(ListsSubMenuCommandFactoryStrategy),
        //ვიზიტების ქვემენიუ
        nameof(VisitsSubMenuCommandFactoryStrategy),
        //ახალი ამოცანის შექმნა
        nameof(NewTaskCommandFactoryStrategy),
        //ამოცანების ჩამონათვალი
        nameof(TasksListFactoryStrategy)
    ];
}
