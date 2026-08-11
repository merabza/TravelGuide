using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence.Configurations;

public sealed class PlaceByLocationConfiguration : IEntityTypeConfiguration<PlaceByLocation>
{
    public void Configure(EntityTypeBuilder<PlaceByLocation> builder)
    {
        const string tableName = "PlacesByLocations";
        builder.ToTable(tableName);

        builder.HasKey(e => new { e.PlaceId, e.LocationId });

        builder.HasOne(d => d.PlaceNavigation).WithMany(p => p.Locations).HasForeignKey(d => d.PlaceId);
        builder.HasOne(d => d.LocationNavigation).WithMany().HasForeignKey(d => d.LocationId);
    }
}
