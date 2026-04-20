using System;
using AppCliTools.CliMenu;
using AppCliTools.CliTools.CliMenuCommands;
using AppCliTools.CliTools.Services.MenuBuilder;
using ParametersManagement.LibParameters;
using TravelGuide.Menu;

namespace TravelGuide;

public sealed class TravelGuideMenuBuilder : IMenuBuilder
{
    private readonly IParametersManager _parametersManager;
    private readonly IServiceProvider _serviceProvider;

    // ReSharper disable once ConvertToPrimaryConstructor
    public TravelGuideMenuBuilder(IServiceProvider serviceProvider, IParametersManager parametersManager)
    {
        _serviceProvider = serviceProvider;
        _parametersManager = parametersManager;
    }

    public CliMenuSet? BuildMainMenu()
    {
        //მთავარი მენიუს ჩატვირთვა
        return CliMenuSetFactory.CreateMenuSet("Main Menu", MenuData.MenuCommandNames,
            _serviceProvider, _parametersManager, true);
    }
}
