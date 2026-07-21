using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TravelGuideDbModels;

namespace TravelGuideDbPersistence.Configurations;

public sealed class TaskModelConfiguration : IEntityTypeConfiguration<TaskModel>
{
    private const int TaskNameLength = 50;

    public void Configure(EntityTypeBuilder<TaskModel> builder)
    {
        const string tableName = "Tasks";
        builder.ToTable(tableName);

        builder.HasKey(e => e.TaskId);
        builder.HasIndex(e => e.TaskName).IsUnique();

        builder.Property(e => e.TaskName).HasMaxLength(TaskNameLength);
    }
}
