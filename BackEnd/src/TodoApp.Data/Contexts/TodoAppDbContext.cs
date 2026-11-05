using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TodoApp.Business;
using TodoApp.Business.Entites;

namespace TodoApp.Data.Contexts;

public class TodoAppDbContext : IdentityDbContext<
	AppUser,                  // ۱. کلاس کاربر
	AppRole,                  // ۲. کلاس نقش
	int,                      // ۳. نوع کلید (Id)
	IdentityUserClaim<int>,   // ۴. Claim کاربر
	AppUserRole,              // ۵. جدول واسط کاربر-نقش
	IdentityUserLogin<int>,   // ۶. Login
	IdentityRoleClaim<int>,   // ۷. Claim نقش
	IdentityUserToken<int>>   // ۸. Token
{
	#region DbSets
	public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
	public DbSet<TaskList> TaskLists => Set<TaskList>();
	public DbSet<TaskListShare> TaskListShares => Set<TaskListShare>();
	public DbSet<TaskItem> TaskItems => Set<TaskItem>();
	public DbSet<Subtask> Subtasks => Set<Subtask>();
	public DbSet<TaskItemAttachment> TaskItemAttachments => Set<TaskItemAttachment>();
	public DbSet<Notification> Notifications => Set<Notification>();
	public DbSet<AccountToken> AccountTokens => Set<AccountToken>();


	#endregion
	#region Ctors
	public TodoAppDbContext(DbContextOptions<TodoAppDbContext> options) : base(options) { }
	#endregion
	#region utilities
	#region OnConfiguring
	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		base.OnConfiguring(optionsBuilder);
	}
	#endregion
	#region OnModelCreating
	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);
		#region User Managment Configoration
		builder.Entity<AppUser>().ToTable("Users");
		builder.Entity<AppRole>().ToTable("Roles");
		builder.Entity<AppUserRole>().ToTable("UserRoles");
		builder.Entity<AppUserRole>(b =>
		{
			b.HasOne(ur => ur.User)
			 .WithMany(u => u.UserRoles)
			 .HasForeignKey(ur => ur.UserId)
			 .OnDelete(DeleteBehavior.Restrict);

			b.HasOne(ur => ur.Role)
			 .WithMany(r => r.UserRoles)
			 .HasForeignKey(ur => ur.RoleId)
			 .OnDelete(DeleteBehavior.Restrict);
		});
		builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
		builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
		builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
		builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");

		builder.Entity<AppUser>(b =>
		{
			b.HasQueryFilter(t => t.IsActive);
			b.Property(u => u.PreferredTheme).HasMaxLength(20);
		});


		builder.Entity<RefreshToken>(b =>
		{
			b.ToTable("RefreshTokens");
			b.Property(x => x.TokenHash).HasMaxLength(64).IsRequired(); // SHA-256 hex = 64 کاراکتر
			b.HasOne(x => x.User)
				.WithMany()
				.HasForeignKey(x => x.UserId)
				.OnDelete(DeleteBehavior.Cascade);
			b.HasIndex(x => x.TokenHash).IsUnique();
			b.HasIndex(x => x.UserId);
		}); 
		#endregion
		builder.ApplyConfigurationsFromAssembly(typeof(TodoAppDbContext).Assembly);
	}
	#endregion
	public override int SaveChanges()
	{
		BeforeSave();
		return base.SaveChanges();
	}
	public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		BeforeSave();
		return base.SaveChangesAsync(cancellationToken);
	}
	private void BeforeSave()
	{
		var now = DateTime.UtcNow;

		foreach (var entry in ChangeTracker.Entries<BaseEntity>())
		{
			switch (entry.State)
			{
				case EntityState.Added:
					entry.Entity.CreatedAt = now;
					break;

				case EntityState.Modified:
					entry.Entity.UpdatedAt = now;
					break;

				case EntityState.Deleted:
					if (entry.Entity is ISoftDeletable)
					{
						entry.State = EntityState.Modified;
						entry.Entity.IsDeleted = true;
						entry.Entity.DeletedAt = now;
					}
					break;
			}
		}
	}
	#endregion
}
