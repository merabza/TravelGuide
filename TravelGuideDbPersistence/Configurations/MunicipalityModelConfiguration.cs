using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence.Configurations;

public sealed class MunicipalityModelConfiguration : IEntityTypeConfiguration<MunicipalityModel>
{
    public const int NameLength = 100;

    public void Configure(EntityTypeBuilder<MunicipalityModel> builder)
    {
        const string tableName = "Municipalities";
        builder.ToTable(tableName);

        builder.HasKey(e => e.MunicipalityId);
        builder.HasIndex(e => e.Name).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(NameLength);
    }
}
