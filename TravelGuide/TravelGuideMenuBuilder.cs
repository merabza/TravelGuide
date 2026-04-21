using System;
using AppCliTools.CliMenu;
using AppCliTools.CliTools.Services.MenuBuilder;
using TravelGuide.Menu;

namespace TravelGuide;

public sealed class TravelGuideMenuBuilder : IMenuBuilder
{
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public TravelGuideMenuBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public CliMenuSet? BuildMainMenu()
    {
        //მთავარი მენიუს ჩატვირთვა
        return CliMenuSetFactory.CreateMenuSet("Main Menu", MenuData.MenuCommandNames, _serviceProvider, true);
    }
}
