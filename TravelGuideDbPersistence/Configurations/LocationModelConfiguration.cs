using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence.Configurations;

public sealed class LocationModelConfiguration : IEntityTypeConfiguration<LocationModel>
{
    public void Configure(EntityTypeBuilder<LocationModel> builder)
    {
        const string tableName = "Locations";
        builder.ToTable(tableName);

        builder.HasKey(e => e.LocationId);

        //ერთი კოორდინატთა წყვილი მხოლოდ ერთხელ ინახება და შეიძლება რამდენიმე ადგილს ეზიარებოდეს
        builder.HasIndex(e => new { e.Latitude, e.Longitude }).IsUnique();
    }
}
