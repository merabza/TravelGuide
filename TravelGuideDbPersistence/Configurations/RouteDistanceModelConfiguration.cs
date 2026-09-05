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

        //ერთი და იგივე ლოკაციათა წყვილისთვის მანძილები მხოლოდ ერთხელ უნდა შეინახოს
        builder.HasIndex(e => new { e.StartLocationId, e.EndLocationId }).IsUnique();

        //ორივე სვეტი Locations-ზე მიუთითებს — ორმაგი კასკადური წაშლა SQL Server-ს გზების გამრავლების გამო არ შეუძლია
        builder.HasOne<LocationModel>().WithMany().HasForeignKey(d => d.StartLocationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<LocationModel>().WithMany().HasForeignKey(d => d.EndLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
