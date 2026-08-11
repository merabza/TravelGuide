//Created by DbContextClassCreator at 7/24/2025 11:44:10 PM

using Microsoft.EntityFrameworkCore;
using SystemTools.DatabaseToolsShared;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence;

public sealed class TravelGuideDbContext : DbContext
{
    public TravelGuideDbContext(DbContextOptions<TravelGuideDbContext> options) : base(options)
    {
    }

    public DbSet<TaskModel> Tasks => Set<TaskModel>();
    public DbSet<TaskStartPoint> TaskStartPoints => Set<TaskStartPoint>();
    public DbSet<PlaceModel> Places => Set<PlaceModel>();
    public DbSet<MonthModel> Months => Set<MonthModel>();
    public DbSet<CategoryModel> Categories => Set<CategoryModel>();
    public DbSet<TagModel> Tags => Set<TagModel>();
    public DbSet<PlaceByBestSeason> PlacesByBestSeasons => Set<PlaceByBestSeason>();
    public DbSet<PlaceByCategory> PlacesByCategories => Set<PlaceByCategory>();
    public DbSet<PlaceByTag> PlacesByTags => Set<PlaceByTag>();
    public DbSet<FromPointModel> FromPoints => Set<FromPointModel>();
    public DbSet<DistanceByPlace> DistanceByPlaces => Set<DistanceByPlace>();
    public DbSet<RegionModel> Regions => Set<RegionModel>();
    public DbSet<MunicipalityModel> Municipalities => Set<MunicipalityModel>();
    public DbSet<MotorcycleModel> Motorcycles => Set<MotorcycleModel>();
    public DbSet<UrlGraphNode> UrlGraphNodes => Set<UrlGraphNode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TravelGuideDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new DatabaseEntitiesDefaultConvention());
    }
}
