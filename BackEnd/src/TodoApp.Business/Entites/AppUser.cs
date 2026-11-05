using Microsoft.AspNetCore.Identity;

namespace TodoApp.Business.Entites;

public class AppUser : IdentityUser<int>
{
	#region Props
	/// <summary>Light / Dark</summary>
	public string PreferredTheme { get; set; } = "Light";

	public bool ReminderEnabled { get; set; } = true;

	public int ReminderDaysBefore { get; set; } = 1;

	/// <summary>تعداد تلاش‌های ناموفق ورود — با هر رمز اشتباه +۱ و با ورود موفق صفر می‌شود (T-023)</summary>
	public int FailedLoginAttempts { get; set; }

	/// <summary>T-024:
	/// هر افزایش = ابطال تمام
	/// Access Token
	/// های صادرشده قبلی
	/// </summary>
	public int JwtVersion { get; set; }

	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	public string? DisplayName { get; set; }
	public bool IsActive { get; set; }
	#endregion
	#region Navigations
	public ICollection<AppUserRole> UserRoles { get; set; } = new List<AppUserRole>(); 
	#endregion
}

public class AppUserRole : IdentityUserRole<int>
{
	public AppUser User { get; set; } = null!;
	public AppRole Role { get; set; } = null!;
}

public class AppRole : IdentityRole<int>
{
	public ICollection<AppUserRole> UserRoles { get; set; } = new List<AppUserRole>();
}
