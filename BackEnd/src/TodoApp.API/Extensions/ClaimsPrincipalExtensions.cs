using System.Security.Claims;

namespace TodoApp.API.Extensions;

public static class ClaimsPrincipalExtensions
{
	/// <summary>
	/// استخراج شناسه عددی کاربر از توکن (ClaimTypes.NameIdentifier — همان claimی که
	/// AuthController فعلی شما در Logout می‌خواند).
	/// </summary>
	public static int GetUserId(this ClaimsPrincipal user)
	{
		var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
		if (!int.TryParse(value, out var userId))
			throw new UnauthorizedAccessException("شناسه کاربر در توکن یافت نشد یا معتبر نیست.");
		return userId;
	}
}
