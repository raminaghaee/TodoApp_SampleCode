using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TodoApp.Business.Entites;

namespace TodoApp.Data.Configurations;

public class AccountTokenConfiguration : IEntityTypeConfiguration<AccountToken>
{
	public void Configure(EntityTypeBuilder<AccountToken> builder)
	{
		builder.ToTable("AccountTokens");

			builder.ToTable("AccountTokens");

			builder.Property(t => t.TokenHash)
				.HasMaxLength(64)
				.IsRequired();

			builder.Property(t => t.Purpose)
				.HasMaxLength(50)
				.IsRequired();

			builder.Property(t => t.PendingEmail)
				.HasMaxLength(256);

			builder.HasIndex(t => t.TokenHash)
				.IsUnique(); 

			builder.HasIndex(t => new { t.UserId, t.Purpose });

	}
}


