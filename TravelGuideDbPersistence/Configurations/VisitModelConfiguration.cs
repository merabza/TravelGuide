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

        //ნავიგაციები საჭირო არ არის — ჩანაწერები პირდაპირ იდენტიფიკატორებით იქმნება
        builder.HasOne<PlaceModel>().WithMany().HasForeignKey(e => e.PlaceId);
        builder.HasOne<MotorcycleModel>().WithMany().HasForeignKey(e => e.MotorcycleId);
    }
}
