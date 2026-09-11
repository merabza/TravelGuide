using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence.Configurations;

public sealed class VisitModelConfiguration : IEntityTypeConfiguration<VisitModel>
{
    public const int CommentLength = 1000;

    public void Configure(EntityTypeBuilder<VisitModel> builder)
    {
        const string tableName = "Visits";
        builder.ToTable(tableName);

        builder.HasKey(e => e.VisitId);

        builder.Property(e => e.Comment).HasMaxLength(CommentLength);

        //ნავიგაციები საჭირო არ არის — ჩანაწერები პირდაპირ იდენტიფიკატორებით იქმნება.
        //ვიზიტი ლოკაციას ებმება და არა ადგილს (Locations საზიარო ჩანაწერებია — PlacesByLocations)
        builder.HasOne<LocationModel>().WithMany().HasForeignKey(e => e.LocationId);
        builder.HasOne<MotorcycleModel>().WithMany().HasForeignKey(e => e.MotorcycleId);
    }
}
