using System;
using System.Threading.Tasks;
using AppCliTools.CliMenu;
using AppCliTools.CliTools.Services.MenuBuilder;
using TravelGuide.Menu;

namespace TravelGuide;

public sealed class TravelGuideMenuBuilder : IMenuBuilder
{
    private readonly IServiceProvider _serviceProvider;

    public TravelGuideMenuBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<CliMenuSet?> BuildMainMenu()
    {
        //მთავარი მენიუს ჩატვირთვა
        return Task.FromResult(CliMenuSetFactory.CreateMenuSet("Main Menu", MenuData.MenuCommandNames, _serviceProvider,
            true));
    }
}
