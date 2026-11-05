using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Business.Entites;

namespace TodoApp.Data.Configurations;

public class SubtaskConfiguration : IEntityTypeConfiguration<Subtask>
{
	public void Configure(EntityTypeBuilder<Subtask> builder)
	{
		builder.ToTable("Subtasks");
		builder.HasKey(x => x.Id);
		builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
		builder.Property(x => x.Position).HasDefaultValue(0);
		builder.HasIndex(x => x.TaskItemId);
		builder.HasOne<TaskItem>()
			.WithMany()
			.HasForeignKey(x => x.TaskItemId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasQueryFilter(t => !t.IsDeleted);
	}
}
