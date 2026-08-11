using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence.Configurations;

public sealed class PlaceModelConfiguration : IEntityTypeConfiguration<PlaceModel>
{
    //Url-ის სიგრძე 850-ს არ უნდა აღემატებოდეს, რადგან SQL Server-ის უნიკალური ინდექსის გასაღების ლიმიტი 1700 ბაიტია
    public const int UrlLength = 500;
    public const int NameLength = 200;

    public void Configure(EntityTypeBuilder<PlaceModel> builder)
    {
        const string tableName = "Places";
        builder.ToTable(tableName);

        builder.HasKey(e => e.PlaceId);
        builder.HasIndex(e => e.Url).IsUnique();

        builder.Property(e => e.Url).HasMaxLength(UrlLength);
        builder.Property(e => e.Name).HasMaxLength(NameLength);

        builder.HasOne(d => d.RegionNavigation).WithMany().HasForeignKey(d => d.RegionId);
        builder.HasOne(d => d.MunicipalityNavigation).WithMany().HasForeignKey(d => d.MunicipalityId);
    }
}
