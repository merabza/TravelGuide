using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence.Configurations;

public sealed class MonthModelConfiguration : IEntityTypeConfiguration<MonthModel>
{
    private const int NameLength = 50;

    public void Configure(EntityTypeBuilder<MonthModel> builder)
    {
        const string tableName = "Months";
        builder.ToTable(tableName);

        builder.HasKey(e => e.MonthId);
        builder.HasIndex(e => e.Name).IsUnique();

        //MonthId კალენდარული თვის ნომერია და კოდიდან ენიჭება, ამიტომ identity არ არის
        builder.Property(e => e.MonthId).ValueGeneratedNever();
        builder.Property(e => e.Name).HasMaxLength(NameLength);
    }
}
