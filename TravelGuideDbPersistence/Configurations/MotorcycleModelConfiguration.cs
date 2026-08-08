using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence.Configurations;

public sealed class MotorcycleModelConfiguration : IEntityTypeConfiguration<MotorcycleModel>
{
    private const int KeyLength = 50;
    private const int ManufacturerLength = 100;
    private const int ModelLength = 100;
    private const int StateNumberLength = 50;

    public void Configure(EntityTypeBuilder<MotorcycleModel> builder)
    {
        const string tableName = "Motorcycles";
        builder.ToTable(tableName);

        builder.HasKey(e => e.MotorcycleId);
        builder.HasIndex(e => e.Key).IsUnique();

        builder.Property(e => e.Key).HasMaxLength(KeyLength);
        builder.Property(e => e.Manufacturer).HasMaxLength(ManufacturerLength);
        builder.Property(e => e.Model).HasMaxLength(ModelLength);
        builder.Property(e => e.StateNumber).HasMaxLength(StateNumberLength);
    }
}
