//Created by ProjectServicesCreatorClassCreator at 7/24/2025 11:44:10 PM

using System.Runtime.CompilerServices;
using AppCliTools.CliParametersDataEdit;
using LibTravelGuideRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParametersManagement.LibDatabaseParameters;
using SystemTools.SystemToolsShared;
using TravelGuideDb;

namespace TravelGuide;

public static class TravelGuideServices
{
    public static IServiceCollection AddServices(this IServiceCollection services,
        DatabaseServerConnections databaseServerConnections, DatabaseParameters? databaseParameters)
    {
        (EDatabaseProvider? dataProvider, string? connectionString, int timeOut) =
            DbConnectionFactory.GetDataProviderConnectionStringCommandTimeOut(databaseParameters,
                databaseServerConnections);

        if (!string.IsNullOrEmpty(connectionString))
        {
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
                case null: break;
                default: throw new SwitchExpressionException();
            }
        }

        services.AddScoped<ITravelGuideRepository, TravelGuideRepository>();
        services.AddSingleton<ITravelGuideRepositoryCreatorFactory, TravelGuideRepositoryCreatorFactory>();
        services.AddHttpClient();

        return services;
    }
}
