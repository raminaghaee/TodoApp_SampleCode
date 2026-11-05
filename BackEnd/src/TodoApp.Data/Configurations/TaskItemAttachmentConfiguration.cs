using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Business.Entites;

namespace TodoApp.Data.Configurations;

public class TaskItemAttachmentConfiguration : IEntityTypeConfiguration<TaskItemAttachment>
{
	public void Configure(EntityTypeBuilder<TaskItemAttachment> builder)
	{
		builder.ToTable("TaskItemAttachments");
		builder.HasKey(x => x.Id);
		builder.Property(x => x.StoragePath).IsRequired().HasMaxLength(500);
		builder.Property(x => x.FileName).IsRequired().HasMaxLength(260);
		builder.Property(x => x.MimeType).HasMaxLength(100);
		builder.Property(x => x.FileData).HasColumnType("varbinary(max)");
		builder.HasIndex(x => x.TaskItemId);
		builder.HasOne<TaskItem>()
			.WithMany()
			.HasForeignKey(x => x.TaskItemId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasQueryFilter(t => !t.IsDeleted);
	}
}
