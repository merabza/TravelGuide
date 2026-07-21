using System.Runtime.CompilerServices;
using AppCliTools.CliMenu;
using AppCliTools.CliParametersDataEdit;
using AppCliTools.CliTools.Services.MenuBuilder;
using DoTravelGuide.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibParameters;
using ParametersManagement.LibParameters.DependencyInjection;
using Serilog.Events;
using SystemTools.SerilogStuff.DependencyInjection;
using SystemTools.SystemToolsShared;
using SystemTools.SystemToolsShared.DependencyInjection;
using TravelGuide.Menu.TravelGuideParametersEdit;
using TravelGuideDbPersistence;
using TravelGuideRepoInterfaces;
using TravelGuideRepositories;

namespace TravelGuide.DependencyInjection;

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
            .AddTransientAllStrategies<IMenuCommandFactoryStrategy>(
                typeof(TravelGuideParametersEditorListCliMenuCommandFactoryStrategy).Assembly)
            .AddDatabase(databaseServerConnections, par.DatabaseParameters)
            .AddApplication(x => { x.AppName = appName; })
            .AddMainParametersManager<ParametersManager>(x =>
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
        (EDatabaseProvider? dataProvider, string? connectionString, int timeOut) =
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
}
