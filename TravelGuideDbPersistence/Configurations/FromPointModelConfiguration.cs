using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence.Configurations;

public sealed class FromPointModelConfiguration : IEntityTypeConfiguration<FromPointModel>
{
    public const int NameLength = 100;

    public void Configure(EntityTypeBuilder<FromPointModel> builder)
    {
        const string tableName = "FromPoints";
        builder.ToTable(tableName);

        builder.HasKey(e => e.FromPointId);
        builder.HasIndex(e => e.Name).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(NameLength);
    }
}
