using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence.Configurations;

public sealed class PlaceModelConfiguration : IEntityTypeConfiguration<PlaceModel>
{
    //Url აღარ ინდექსირდება — სიგრძის ზღვარი მხოლოდ სვეტის ზომაა და ზედმეტად გრძელი მისამართების გამოსატოვებლად გამოიყენება
    public const int UrlLength = 500;
    public const int NameLength = 200;

    public void Configure(EntityTypeBuilder<PlaceModel> builder)
    {
        const string tableName = "Places";
        builder.ToTable(tableName);

        builder.HasKey(e => e.PlaceId);

        //Url-ის მაგივრად მისი დეტერმინისტული ხეშ-კოდი ინდექსირდება — ინდექსი არაუნიკალურია (სხვადასხვა Url-ს
        //იშვიათად ერთი ხეში შეიძლება ჰქონდეს); Url-ის უნიკალურობას აპლიკაცია იცავს შენახვამდე შემოწმებით
        builder.HasIndex(e => e.UrlHashCode);

        builder.Property(e => e.Url).HasMaxLength(UrlLength);
        builder.Property(e => e.Name).HasMaxLength(NameLength);

        builder.HasOne(d => d.RegionNavigation).WithMany().HasForeignKey(d => d.RegionId);
        builder.HasOne(d => d.MunicipalityNavigation).WithMany().HasForeignKey(d => d.MunicipalityId);
    }
}
