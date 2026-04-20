using System;
using System.Runtime.CompilerServices;
using AppCliTools.CliMenu;
using AppCliTools.CliMenu.DependencyInjection;
using AppCliTools.CliParametersDataEdit;
using AppCliTools.CliTools.DependencyInjection;
using AppCliTools.CliTools.Models;
using AppCliTools.CliTools.Services.MenuBuilder;
using DoTravelGuide.Models;
using LibTravelGuideRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibParameters;
using Serilog.Events;
using SystemTools.SystemToolsShared;
using TravelGuide.Menu.TravelGuideParametersEdit;
using TravelGuideDb;

namespace TravelGuide;

public static class TravelGuideServices
{
    public static IServiceCollection AddServices(this IServiceCollection services, string appName,
        TravelGuideParameters par, string parametersFileName)
    {
        var databaseServerConnections = new DatabaseServerConnections(par.DatabaseServerConnections);

        // @formatter:off
        services
            .AddSerilogLoggerService(LogEventLevel.Information, appName, par.LogFolder)
            .AddScoped<ITravelGuideRepository, TravelGuideRepository>()
            .AddSingleton<ITravelGuideRepositoryCreatorFactory, TravelGuideRepositoryCreatorFactory>()
            .AddHttpClient()
            .AddSingleton<IMenuBuilder, TravelGuideMenuBuilder>()
            .AddMenuCommandsFactoryStrategies()
            .AddDatabase(databaseServerConnections, par.DatabaseParameters)
            .AddApplication(x => { x.AppName = appName; })
            .AddMainParametersManager(x =>
            {
                x.ParametersFileName = parametersFileName;
                x.Par = par;
            });
        // @formatter:on

        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services,
        DatabaseServerConnections databaseServerConnections, DatabaseParameters? databaseParameters)
    {
        var (dataProvider, connectionString, timeOut) =
            DbConnectionFactory.GetDataProviderConnectionStringCommandTimeOut(databaseParameters,
                databaseServerConnections);

        if (string.IsNullOrEmpty(connectionString))
        {
            return services;
        }

        switch (dataProvider)
        {
            case EDatabaseProvider.SqlServer:
                services.AddDbContext<TravelGuideDbContext>(options =>
                    options.UseSqlServer(connectionString, con => con.CommandTimeout(timeOut)));
                break;
            case EDatabaseProvider.None:
            case EDatabaseProvider.SqLite:
            case EDatabaseProvider.OleDb:
            case EDatabaseProvider.WebAgent:
            case null:
                break;
            default:
                throw new SwitchExpressionException();
        }

        return services;
    }

    private static IServiceCollection AddMenuCommandsFactoryStrategies(this IServiceCollection services)
    {
        services.AddTransientAllStrategies<IMenuCommandFactoryStrategy>(
            typeof(TravelGuideParametersEditorListCliMenuCommandFactoryStrategy).Assembly);
        return services;
    }

    private static IServiceCollection AddMainParametersManager(this IServiceCollection services,
        Action<MainParametersManagerOptions> setupAction)
    {
        services.AddSingleton<IParametersManager, ParametersManager>();
        services.Configure(setupAction);
        return services;
    }

    private static IServiceCollection AddApplication(this IServiceCollection services,
        Action<ApplicationOptions> setupAction)
    {
        services.AddSingleton<IApplication, TravelGuideApplication>();
        services.Configure(setupAction);
        return services;
    }
}
