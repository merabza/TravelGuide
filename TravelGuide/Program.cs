//Created by ConsoleProgramClassCreator at 7/24/2025 11:44:10 PM

using System;
using System.Net.Http;
using CliParameters;
using DoTravelGuide.Models;
using LibParameters;
using LibTravelGuideRepositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using SystemToolsShared;
using TravelGuide;

ILogger<Program>? logger = null;
try
{
    Console.WriteLine("Loading...");

    const string appName = "TravelGuide";

    //პროგრამის ატრიბუტების დაყენება 
    ProgramAttributes.Instance.AppName = appName;

    var argParser = new ArgumentsParser<TravelGuideParameters>(args, appName, null);
    switch (argParser.Analysis())
    {
        case EParseResult.Ok: break;
        case EParseResult.Usage: return 1;
        case EParseResult.Error: return 2;
        default: throw new ArgumentOutOfRangeException();
    }

    var par = (TravelGuideParameters?)argParser.Par;
    if (par is null)
    {
        StShared.WriteErrorLine("ConsoleTestParameters is null", true);
        return 3;
    }

    var parametersFileName = argParser.ParametersFileName;
    var servicesCreator = new TravelGuideServicesCreator(par);
    // ReSharper disable once using
    var serviceProvider = servicesCreator.CreateServiceProvider(LogEventLevel.Information);

    if (serviceProvider == null)
    {
        StShared.WriteErrorLine("Logger not created", true);
        return 4;
    }

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

    var travelGuide = new TravelGuide.TravelGuide(logger, httpClientFactory,
        new ParametersManager(parametersFileName, par));

    return travelGuide.Run() ? 0 : 1;
}
catch (Exception e)
{
    StShared.WriteException(e, true, logger);
    return 7;
}
finally
{
    Log.CloseAndFlush();
}