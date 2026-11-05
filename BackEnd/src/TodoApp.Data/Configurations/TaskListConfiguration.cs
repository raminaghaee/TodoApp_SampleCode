using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Business.Entites;

namespace TodoApp.Data.Configurations;

public class TaskListConfiguration : IEntityTypeConfiguration<TaskList>
{
	public void Configure(EntityTypeBuilder<TaskList> builder)
	{
		builder.ToTable("TaskLists");
		builder.HasKey(x => x.Id);
		builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
		builder.Property(x => x.Color).IsRequired().HasMaxLength(20).HasDefaultValue("blue");
		builder.HasIndex(x => x.OwnerId);

		builder.HasQueryFilter(t => !t.IsDeleted);
	}
}
