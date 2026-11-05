using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Business.Entites;

namespace TodoApp.Data.Configurations;

public class TaskListShareConfiguration : IEntityTypeConfiguration<TaskListShare>
{
	public void Configure(EntityTypeBuilder<TaskListShare> builder)
	{
		builder.ToTable("TaskListShares");
		builder.HasKey(x => x.Id);
		builder.HasIndex(x => new { x.TaskListId, x.UserId }).IsUnique();

		builder.HasIndex(x => x.UserId);

		builder.HasOne(x => x.TaskList)
			.WithMany(l => l.TaskListShares)
			.HasForeignKey(x => x.TaskListId)
			.OnDelete(DeleteBehavior.Restrict);
		builder.HasOne(x => x.AppUser)
			.WithMany()
			.HasForeignKey(x => x.UserId)
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasQueryFilter(t => !t.IsDeleted);
	}
}
