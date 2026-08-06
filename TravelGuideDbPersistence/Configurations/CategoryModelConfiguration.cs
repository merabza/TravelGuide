using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence.Configurations;

public sealed class CategoryModelConfiguration : IEntityTypeConfiguration<CategoryModel>
{
    public const int NameLength = 100;

    public void Configure(EntityTypeBuilder<CategoryModel> builder)
    {
        const string tableName = "Categories";
        builder.ToTable(tableName);

        builder.HasKey(e => e.CategoryId);
        builder.HasIndex(e => e.Name).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(NameLength);
    }
}
