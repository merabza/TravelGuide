using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence.Configurations;

public sealed class UrlGraphNodeConfiguration : IEntityTypeConfiguration<UrlGraphNode>
{
    public void Configure(EntityTypeBuilder<UrlGraphNode> builder)
    {
        const string tableName = "UrlGraphNodes";
        builder.ToTable(tableName);

        builder.HasKey(e => e.UgnId);

        //ერთი და იგივე კავშირი (რომელ გვერდზე რომელი მისამართი მოიძებნა) მხოლოდ ერთხელ უნდა შეინახოს
        builder.HasIndex(e => new { e.FromUrlId, e.GotUrlId }).IsUnique();

        //ორივე სვეტი Places-ზე მიუთითებს — ორმაგი კასკადური წაშლა SQL Server-ს გზების გამრავლების გამო არ შეუძლია
        builder.HasOne<PlaceModel>().WithMany().HasForeignKey(d => d.FromUrlId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlaceModel>().WithMany().HasForeignKey(d => d.GotUrlId).OnDelete(DeleteBehavior.Restrict);
    }
}
