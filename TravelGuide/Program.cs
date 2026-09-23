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
using TravelGuide.DependencyInjection;

ILogger<Program>? logger = null;
try
{
    Console.WriteLine("Loading...");

    const string appName = "Travel Guide";

    var argumentsAnalyzer = new ArgumentsAnalyzer();

    if (!await argumentsAnalyzer.Analysis(args))
    {
        return argumentsAnalyzer.ExitCode;
    }

    var argParser = new ParametersService<TravelGuideParameters>(appName);

    switch (argParser.Analysis(argumentsAnalyzer.ParametersFileName))
    {
        case EParseResult.Ok:
            break;
        case EParseResult.ShowHelp:
            argumentsAnalyzer.ShowHelp();
            return 1;
        case EParseResult.ParseError:
            StShared.WriteErrorLine($"File {argumentsAnalyzer.ParametersFileName} is not valid", true, logger, false);
            return 2;
        default:
            throw new SwitchExpressionException();
    }

    var serviceCollection = new ServiceCollection();

    // ReSharper disable once using
    await using ServiceProvider serviceProvider = serviceCollection
        .AddServices(appName, argParser.Par!, argParser.ParametersFileName!).BuildServiceProvider();

    (CliAppLoopParameters? cliLoopPar, logger) = CliAppLoopParameters.Create<Program>(serviceProvider);
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
