using System.Diagnostics.CodeAnalysis;
using Hangfire.Dashboard;

namespace TodoApp.API.Configuration;


/// <summary>
/// Authorization برای Hangfire Dashboard:
/// - Development: دسترسی آزاد (طبق DoD تسک)
/// - Production: فقط کاربر احراز هویت‌شده با نقش Admin
/// </summary>
public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
	private readonly IWebHostEnvironment _env;

	public HangfireDashboardAuthFilter(IWebHostEnvironment env) => _env = env;

	public bool Authorize([NotNull] DashboardContext context)
	{
		if (_env.IsDevelopment())
			return true;

		var user = context.GetHttpContext().User;
		return user.Identity?.IsAuthenticated == true && user.IsInRole("Admin");
	}
}
