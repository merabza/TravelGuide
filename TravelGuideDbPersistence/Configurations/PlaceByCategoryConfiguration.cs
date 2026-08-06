using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence.Configurations;

public sealed class PlaceByCategoryConfiguration : IEntityTypeConfiguration<PlaceByCategory>
{
    public void Configure(EntityTypeBuilder<PlaceByCategory> builder)
    {
        const string tableName = "PlacesByCategories";
        builder.ToTable(tableName);

        builder.HasKey(e => new { e.PlaceId, e.CategoryId });

        builder.HasOne(d => d.PlaceNavigation).WithMany(p => p.Categories).HasForeignKey(d => d.PlaceId);
        builder.HasOne(d => d.CategoryNavigation).WithMany().HasForeignKey(d => d.CategoryId);
    }
}
