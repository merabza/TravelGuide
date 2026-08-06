using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence.Configurations;

public sealed class TagModelConfiguration : IEntityTypeConfiguration<TagModel>
{
    public const int NameLength = 100;

    public void Configure(EntityTypeBuilder<TagModel> builder)
    {
        const string tableName = "Tags";
        builder.ToTable(tableName);

        builder.HasKey(e => e.TagId);
        builder.HasIndex(e => e.Name).IsUnique();

        builder.Property(e => e.Name).HasMaxLength(NameLength);
    }
}
