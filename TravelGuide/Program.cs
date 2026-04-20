//Created by ConsoleProgramClassCreator at 7/24/2025 11:44:10 PM

using System;
using System.Runtime.CompilerServices;
using AppCliTools.CliParameters;
using AppCliTools.CliTools;
using DoTravelGuide.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using SystemTools.SystemToolsShared;
using TravelGuide;

ILogger<Program>? logger = null;
try
{
    Console.WriteLine("Loading...");

    const string appName = "Travel Guide";

    var argParser = new ArgumentsParser<TravelGuideParameters>(args, appName);

    switch (argParser.Analysis())
    {
        case EParseResult.Ok: break;
        case EParseResult.Usage: return 1;
        case EParseResult.Error: return 2;
        default: throw new SwitchExpressionException();
    }

    var serviceCollection = new ServiceCollection();

    // ReSharper disable once using
    await using var serviceProvider = serviceCollection
        .AddServices(appName, argParser.Par!, argParser.ParametersFileName!).BuildServiceProvider();

    //var databaseServerConnections = new DatabaseServerConnections(par.DatabaseServerConnections);

    //serviceCollection.AddSerilogLoggerService(LogEventLevel.Information, appName, par.LogFolder)
    //    .AddMenuCommandsFactoryStrategies().AddDatabase(databaseServerConnections, par.DatabaseParameters).AddServices()
    //    .AddApplication(x =>
    //    {
    //        x.AppName = appName;
    //    }).AddMainParametersManager(x =>
    //    {
    //        x.ParametersFileName = parametersFileName;
    //        x.Par = par;
    //    });

    //// ReSharper disable once using
    //await using ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

    //logger = serviceProvider.GetService<ILogger<Program>>();
    //if (logger is null)
    //{
    //    StShared.WriteErrorLine("logger is null", true);
    //    return 5;
    //}

    var cliLoopPar = CliAppLoopParameters.Create<Program>(serviceProvider);
    if (cliLoopPar is null)
    {
        return 6;
    }

    var travelGuide = new CliAppLoop(cliLoopPar);

    return await travelGuide.Run() ? 0 : 100;
}
catch (Exception e)
{
    StShared.WriteException(e, true, logger);
    return 7;
}
finally
{
    await Log.CloseAndFlushAsync();
}
