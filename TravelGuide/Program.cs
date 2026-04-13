//Created by ConsoleProgramClassCreator at 7/24/2025 11:44:10 PM

using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using AppCliTools.CliParameters;
using AppCliTools.CliTools;
using AppCliTools.CliTools.DependencyInjection;
using AppCliTools.CliTools.Services.MenuBuilder;
using DoTravelGuide.Models;
using LibTravelGuideRepositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibParameters;
using Serilog;
using Serilog.Events;
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

    var par = (TravelGuideParameters?)argParser.Par;
    if (par is null)
    {
        StShared.WriteErrorLine("TravelGuideParameters is null", true);
        return 3;
    }

    string? parametersFileName = argParser.ParametersFileName;
    if (string.IsNullOrWhiteSpace(parametersFileName))
    {
        StShared.WriteErrorLine("parametersFileName is null or empty", true);
        return 3;
    }

    var serviceCollection = new ServiceCollection();

    var databaseServerConnections = new DatabaseServerConnections(par.DatabaseServerConnections);

    serviceCollection.AddSerilogLoggerService(LogEventLevel.Information, appName, par.LogFolder)
        .AddServices(databaseServerConnections, par.DatabaseParameters);

    // ReSharper disable once using
    await using ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

    logger = serviceProvider.GetService<ILogger<Program>>();
    if (logger is null)
    {
        StShared.WriteErrorLine("logger is null", true);
        return 5;
    }

    var travelGuideRepositoryCreatorFactory = serviceProvider.GetService<ITravelGuideRepositoryCreatorFactory>();
    if (travelGuideRepositoryCreatorFactory is null)
    {
        StShared.WriteErrorLine("travelGuideRepositoryCreatorFactory is null", true);
        return 6;
    }

    var httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
    if (httpClientFactory is null)
    {
        StShared.WriteErrorLine("httpClientFactory is null", true);
        return 6;
    }

    var travelGuide = new CliAppLoop(app, menuBuilder, logger, httpClientFactory,
        new ParametersManager(parametersFileName, par));

    return await travelGuide.Run() ? 0 : 1;
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
