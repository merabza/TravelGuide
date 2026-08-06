using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence.Configurations;

public sealed class PlaceByBestSeasonConfiguration : IEntityTypeConfiguration<PlaceByBestSeason>
{
    public void Configure(EntityTypeBuilder<PlaceByBestSeason> builder)
    {
        const string tableName = "PlacesByBestSeasons";
        builder.ToTable(tableName);

        builder.HasKey(e => new { e.PlaceId, e.MonthId });

        builder.HasOne(d => d.PlaceNavigation).WithMany(p => p.BestSeasons).HasForeignKey(d => d.PlaceId);
        builder.HasOne(d => d.MonthNavigation).WithMany().HasForeignKey(d => d.MonthId);
    }
}
