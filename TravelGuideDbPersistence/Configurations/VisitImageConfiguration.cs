using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence.Configurations;

public sealed class VisitImageConfiguration : IEntityTypeConfiguration<VisitImage>
{
    public const int FileNameLength = 260;

    public void Configure(EntityTypeBuilder<VisitImage> builder)
    {
        const string tableName = "VisitImages";
        builder.ToTable(tableName);

        builder.HasKey(e => e.VisitImageId);

        //ერთ ვიზიტზე ერთი ფაილი მხოლოდ ერთხელ უნდა იყოს მიბმული
        builder.HasIndex(e => new { e.VisitId, e.FileName }).IsUnique();

        //სიგრძის შეზღუდვის გარეშე nvarchar(max) იქნებოდა და ინდექსში ვერ მოხვდებოდა
        builder.Property(e => e.FileName).HasMaxLength(FileNameLength);

        builder.HasOne(d => d.VisitNavigation).WithMany(p => p.Images).HasForeignKey(d => d.VisitId);
    }
}
