//Created by ProjectServicesCreatorClassCreator at 7/24/2025 11:44:10 PM

using System;
using System.Runtime.CompilerServices;
using AppCliTools.CliMenu;
using AppCliTools.CliParametersDataEdit;
using AppCliTools.CliTools.Models;
using AppCliTools.CliTools.Services.MenuBuilder;
using LibTravelGuideRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParametersManagement.LibDatabaseParameters;
using ParametersManagement.LibParameters;
using SystemTools.SystemToolsShared;
using TravelGuide.Menu.TravelGuideParametersEdit;
using TravelGuideDb;

namespace TravelGuide;

public static class TravelGuideServices
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<ITravelGuideRepository, TravelGuideRepository>();
        services.AddSingleton<ITravelGuideRepositoryCreatorFactory, TravelGuideRepositoryCreatorFactory>();
        services.AddHttpClient();
        services.AddSingleton<IMenuBuilder, TravelGuideMenuBuilder>();

        return services;
    }

    public static IServiceCollection AddDatabase(this IServiceCollection services,
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

    public static IServiceCollection AddMenuCommandsFactoryStrategies(this IServiceCollection services)
    {
        services.AddTransientAllMenuCommandFactoryStrategies(
            typeof(TravelGuideParametersEditorListCliMenuCommandFactoryStrategy).Assembly);
        return services;
    }

    public static IServiceCollection AddMainParametersManager(this IServiceCollection services,
        Action<MainParametersManagerOptions> setupAction)
    {
        services.AddSingleton<IParametersManager, ParametersManager>();
        services.Configure(setupAction);
        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services,
        Action<ApplicationOptions> setupAction)
    {
        services.AddSingleton<IApplication, TravelGuideApplication>();
        services.Configure(setupAction);
        return services;
    }
}
