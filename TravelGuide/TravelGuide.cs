//Created by ProjectMainClassCreatorForCliAppWithMenu at 7/24/2025 11:44:10 PM

using System;
using System.Linq;
using System.Net.Http;
using CliMenu;
using CliParameters.CliMenuCommands;
using CliTools;
using CliTools.CliMenuCommands;
using DoTravelGuide.Models;
using LibDataInput;
using LibParameters;
using Microsoft.Extensions.Logging;
using TravelGuide.MenuCommands;

namespace TravelGuide;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class TravelGuide : CliAppLoop
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;

    private readonly ParametersManager _parametersManager;

    //ეს საჭიროა იმ შემთხვევაში, თუ ბაზაში რედაქტორები გვინდა გავაკეთოთ და საჭიროა, რომ ამ მენიუდან მოხდეს გამოძახება
    //private readonly ITravelGuideRepositoryCreatorFactory _travelGuideRepositoryCreatorFactory;

    public TravelGuide(ILogger logger, IHttpClientFactory httpClientFactory, ParametersManager parametersManager)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _parametersManager = parametersManager;
        ////_travelGuideRepositoryCreatorFactory = travelGuideRepositoryCreatorFactory;
    }

    public override CliMenuSet BuildMainMenu()
    {
        var parameters = (TravelGuideParameters)_parametersManager.Parameters;

        var mainMenuSet = new CliMenuSet("Main Menu");

        //ძირითადი პარამეტრების რედაქტირება
        var travelGuideParametersEditor =
            new TravelGuideParametersEditor(parameters, _parametersManager, _logger, _httpClientFactory);
        mainMenuSet.AddMenuItem(new ParametersEditorListCliMenuCommand(travelGuideParametersEditor));

        //საჭირო მენიუს ელემენტები

        var newAppTaskCommand = new NewTaskCommand(_parametersManager);
        mainMenuSet.AddMenuItem(newAppTaskCommand);
        foreach (var kvp in parameters.Tasks.OrderBy(o => o.Key))
            mainMenuSet.AddMenuItem(new TaskSubMenuCommand(_logger, _parametersManager, kvp.Key));

        //პროგრამიდან გასასვლელი
        var key = ConsoleKey.Escape.Value().ToLower();
        mainMenuSet.AddMenuItem(key, new ExitCliMenuCommand(), key.Length);

        return mainMenuSet;
    }
}