using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence.Configurations;

public sealed class DistanceByPlaceConfiguration : IEntityTypeConfiguration<DistanceByPlace>
{
    public void Configure(EntityTypeBuilder<DistanceByPlace> builder)
    {
        const string tableName = "DistanceByPlaces";
        builder.ToTable(tableName);

        builder.HasKey(e => e.DistanceByPlaceId);

        //ერთი ადგილიდან ერთ საწყის წერტილამდე მხოლოდ ერთი მანძილი უნდა არსებობდეს
        builder.HasIndex(e => new { e.PlaceId, e.FromPointId }).IsUnique();

        builder.HasOne(d => d.PlaceNavigation).WithMany(p => p.Distances).HasForeignKey(d => d.PlaceId);
        builder.HasOne(d => d.FromPointNavigation).WithMany().HasForeignKey(d => d.FromPointId);
    }
}
