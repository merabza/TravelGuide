using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence.Configurations;

public sealed class RegionModelConfiguration : IEntityTypeConfiguration<RegionModel>
{
    public const int NameLength = 100;

    public void Configure(EntityTypeBuilder<RegionModel> builder)
    {
        const string tableName = "Regions";
        builder.ToTable(tableName);

        builder.HasKey(e => e.RegionId);
        builder.HasIndex(e => e.Name).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(NameLength);
    }
}
