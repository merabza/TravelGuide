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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TravelGuideDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new DatabaseEntitiesDefaultConvention());
    }
}
