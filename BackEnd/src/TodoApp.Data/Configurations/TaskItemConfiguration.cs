using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Business.Entites;

namespace TodoApp.Data.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
	public void Configure(EntityTypeBuilder<TaskItem> builder)
	{
		builder.ToTable("TaskItems");
		builder.HasKey(x => x.Id);
		builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
		builder.Property(x => x.Notes).HasMaxLength(int.MaxValue);
		builder.HasIndex(x => x.TaskListId);
		builder.HasIndex(x => x.AssignedToId);
		builder.HasIndex(x => x.CreatedById);
		builder.HasOne(x => x.TaskList)
			.WithMany(l => l.TaskItems)
			.HasForeignKey(x => x.TaskListId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasQueryFilter(t => !t.IsDeleted);
	}
}
