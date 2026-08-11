using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence.Configurations;

public sealed class RouteDistanceModelConfiguration : IEntityTypeConfiguration<RouteDistanceModel>
{
    public void Configure(EntityTypeBuilder<RouteDistanceModel> builder)
    {
        const string tableName = "RouteDistances";
        builder.ToTable(tableName);

        builder.HasKey(e => e.RouteDistanceId);

        //ერთი და იგივე წერტილთა წყვილისთვის მანძილები მხოლოდ ერთხელ უნდა შეინახოს
        builder.HasIndex(e => new { e.StartLatitude, e.StartLongitude, e.EndLatitude, e.EndLongitude }).IsUnique();
    }
}
