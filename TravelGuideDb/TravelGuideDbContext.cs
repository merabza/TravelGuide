//Created by DbContextClassCreator at 7/24/2025 11:44:10 PM

using TravelGuideDb.Models;
using SystemToolsShared;
using Microsoft.EntityFrameworkCore;

namespace TravelGuideDb;

public sealed class TravelGuideDbContext : DbContext
{
    public TravelGuideDbContext(DbContextOptions options, bool isDesignTime) : base(options)
    {
    }

    public TravelGuideDbContext(DbContextOptions<TravelGuideDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestModel>(entity =>
        {
            string tableName = nameof(TestModel).Pluralize();
            entity.HasKey(e => e.TestId);
            entity.ToTable(tableName.UnCapitalize());
            entity.HasIndex(e => e.TestName)
                .HasDatabaseName($"IX_{tableName}_{nameof(TestModel.TestName).UnCapitalize()}").IsUnique();
            entity.Property(e => e.TestId).HasColumnName(nameof(TestModel.TestId).UnCapitalize());
            entity.Property(e => e.TestName).HasColumnName(nameof(TestModel.TestName).UnCapitalize()).HasMaxLength(50);
        });
    }
}