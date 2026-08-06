using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence.Configurations;

public sealed class PlaceByTagConfiguration : IEntityTypeConfiguration<PlaceByTag>
{
    public void Configure(EntityTypeBuilder<PlaceByTag> builder)
    {
        const string tableName = "PlacesByTags";
        builder.ToTable(tableName);

        builder.HasKey(e => new { e.PlaceId, e.TagId });

        builder.HasOne(d => d.PlaceNavigation).WithMany(p => p.Tags).HasForeignKey(d => d.PlaceId);
        builder.HasOne(d => d.TagNavigation).WithMany().HasForeignKey(d => d.TagId);
    }
}
