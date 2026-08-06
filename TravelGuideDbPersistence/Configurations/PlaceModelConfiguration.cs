using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence.Configurations;

public sealed class PlaceModelConfiguration : IEntityTypeConfiguration<PlaceModel>
{
    //Url-ის სიგრძე 850-ს არ უნდა აღემატებოდეს, რადგან SQL Server-ის უნიკალური ინდექსის გასაღების ლიმიტი 1700 ბაიტია
    public const int UrlLength = 500;
    public const int NameLength = 200;
    public const int RegionLength = 100;
    public const int MunicipalityLength = 100;
    public const int BestSeasonLength = 200;

    public void Configure(EntityTypeBuilder<PlaceModel> builder)
    {
        const string tableName = "Places";
        builder.ToTable(tableName);

        builder.HasKey(e => e.PlaceId);
        builder.HasIndex(e => e.Url).IsUnique();

        builder.Property(e => e.Url).HasMaxLength(UrlLength);
        builder.Property(e => e.Name).HasMaxLength(NameLength);
        builder.Property(e => e.Region).HasMaxLength(RegionLength);
        builder.Property(e => e.Municipality).HasMaxLength(MunicipalityLength);
        builder.Property(e => e.BestSeason).HasMaxLength(BestSeasonLength);

        //პრიმიტიული კოლექციები JSON ტექსტად ინახება; ტიპი ცხადად ეთითება, რომ EF-მა SQL Server-ის json ტიპი არ აირჩიოს
        builder.Property(e => e.Categories).HasColumnType("nvarchar(max)");
        builder.Property(e => e.Tags).HasColumnType("nvarchar(max)");
        builder.Property(e => e.Distances).HasColumnType("nvarchar(max)");
    }
}
