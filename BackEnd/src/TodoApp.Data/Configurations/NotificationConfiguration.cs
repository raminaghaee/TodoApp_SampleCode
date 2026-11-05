using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Business.Entites;

namespace TodoApp.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
	public void Configure(EntityTypeBuilder<Notification> builder)
	{
		builder.ToTable("Notifications");
		builder.HasKey(x => x.Id);
		builder.Property(x => x.Type).IsRequired().HasMaxLength(50).HasDefaultValue("info");
		builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
		builder.Property(x => x.Body).HasMaxLength(int.MaxValue);
		builder.HasIndex(x => new { x.UserId, x.ReadAt });
		builder.HasIndex(x => x.TaskItemId);
		builder.HasOne<TaskItem>()
			.WithMany()
			.HasForeignKey(x => x.TaskItemId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasQueryFilter(t => !t.IsDeleted);
	}
}
