//Created by ProjectServicesCreatorClassCreator at 7/24/2025 11:44:10 PM

using System;
using CliParametersDataEdit;
using LibDatabaseParameters;
using TravelGuideDb;
using DoTravelGuide.Models;
using LibTravelGuideRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SystemToolsShared;

namespace TravelGuide;

public sealed class TravelGuideServicesCreator : ServicesCreator
{
    private readonly TravelGuideParameters _par;

    // ReSharper disable once ConvertToPrimaryConstructor
    public TravelGuideServicesCreator(TravelGuideParameters par) : base(par.LogFolder, null, "TravelGuide")
    {
        _par = par;
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);

        var databaseServerConnections = new DatabaseServerConnections(_par.DatabaseServerConnections);

        var (dataProvider, connectionString) =
            DbConnectionFactory.GetDataProviderAndConnectionString(_par.DatabaseParameters, databaseServerConnections);

        if (!string.IsNullOrEmpty(connectionString))
        {
            switch (dataProvider)
            {
                case EDatabaseProvider.SqlServer:
                    services.AddDbContext<TravelGuideDbContext>(options => options.UseSqlServer(connectionString));
                    break;
                case EDatabaseProvider.None:
                case EDatabaseProvider.SqLite:
                case EDatabaseProvider.OleDb:
                case EDatabaseProvider.WebAgent:
                case null: break;
                default: throw new ArgumentOutOfRangeException(nameof(dataProvider));
            }
        }

        services.AddScoped<ITravelGuideRepository, TravelGuideRepository>();
        services.AddSingleton<ITravelGuideRepositoryCreatorFactory, TravelGuideRepositoryCreatorFactory>();
        services.AddHttpClient();
    }
}