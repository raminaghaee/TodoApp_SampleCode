
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace TodoApp.API.Extensions;

/// <summary>
/// پیکربندی Rate Limiting
/// محافظت از سرور در برابر Brute Force / DDoS با RateLimiter داخلی ASP.NET Core (بدون پکیج خارجی).
/// </summary>
public static class RateLimitingExtensions
{
	// ---- نام Policy ها ----
	public const string LoginPolicy = "login";
	public const string GlobalPolicy = "global";   // از طریق GlobalLimiter روی همه Endpoint ها (بدون attribute)
	public const string SearchPolicy = "search";
	public const string AuthPolicy = "auth";     // attribute «auth» از قبل روی Refresh (T-024) هست — باید ثبت شود
	public const string AccountSensitivePolicy = "account-sensitive";
	public const string AccountRecoveryPolicy = "account-recovery";


	// ---- تک‌منبعِ تنظمیات (برای ساخت Policy + Fallback هدرها) ----
	private static readonly (int Limit, TimeSpan Window) LoginSettings = (5, TimeSpan.FromMinutes(15));
	private static readonly (int Limit, TimeSpan Window) GlobalSettings = (100, TimeSpan.FromMinutes(1));
	private static readonly (int Limit, TimeSpan Window) SearchSettings = (30, TimeSpan.FromMinutes(1));
	private static readonly (int Limit, TimeSpan Window) AuthSettings = (10, TimeSpan.FromMinutes(1));
	private static readonly (int Limit, TimeSpan Window) AccountSensitiveSettings = (3, TimeSpan.FromMinutes(15));
	private static readonly (int Limit, TimeSpan Window) AccountRecoverySettings = (5, TimeSpan.FromMinutes(15));



	/// <summary>ثبت RateLimiter در DI — در Program.cs: builder.Services.AddRateLimiting();</summary>
	public static IServiceCollection AddRateLimiting(this IServiceCollection services)
	{
		services.AddRateLimiter(options =>
		{
			options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

			// --------------------------------------------------------------
			// "global" — ۱۰۰ درخواست در دقیقه — روی «همه» Endpoint ها
			// Partition Key: UserId (اگر احراز هویت شده) وگرنه IP.
			// UserId اولویت دارد تا کاربران پشت NAT (IP مشترک) سهمیه جدا داشته باشند.
			// --------------------------------------------------------------
			options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
				RateLimitPartition.GetFixedWindowLimiter(
					GetPartitionKey(httpContext),
					_ => new FixedWindowRateLimiterOptions
					{
						PermitLimit = GlobalSettings.Limit,
						Window = GlobalSettings.Window,
						QueueLimit = 0 // بدون صف → درخواست اضافه بلافاصله 429
					}));

			// --------------------------------------------------------------
			// "login" — ۵ درخواست در ۱۵ دقیقه — به ازای هر IP (جلوگیری از Brute Force)
			// Endpoint لاگین احراز هویت نشده است و فقط IP در دسترس است.
			// --------------------------------------------------------------
			options.AddPolicy(LoginPolicy, httpContext =>
				RateLimitPartition.GetFixedWindowLimiter(
					GetClientIp(httpContext),
					_ => new FixedWindowRateLimiterOptions
					{
						PermitLimit = LoginSettings.Limit,
						Window = LoginSettings.Window,
						QueueLimit = 0
					}));

			// --------------------------------------------------------------
			// "search" — ۳۰ درخواست در دقیقه — به ازای هر UserId (Query سنگین)
			// --------------------------------------------------------------
			options.AddPolicy(SearchPolicy, httpContext =>
				RateLimitPartition.GetFixedWindowLimiter(
					GetPartitionKey(httpContext),
					_ => new FixedWindowRateLimiterOptions
					{
						PermitLimit = SearchSettings.Limit,
						Window = SearchSettings.Window,
						QueueLimit = 0
					}));


			options.AddPolicy(AuthPolicy, httpContext =>
				RateLimitPartition.GetFixedWindowLimiter(
					GetClientIp(httpContext),
					_ => new FixedWindowRateLimiterOptions
					{
						PermitLimit = AuthSettings.Limit,
						Window = AuthSettings.Window,
						QueueLimit = 0
					}));

			// --------------------------------------------------------------
			// "account-sensitive" — ۳ در ۱۵ دقیقه — به ازای هر UserId
			// برای عملیات حساس روی حساب: تغییر ایمیل، تغییر رمز، حذف حساب.
			// کلید partition اولویت UserId دارد (کاربر لاگین است)، ولی اگر
			// به هر دلیل احراز هویت نشده بود، IP استفاده می‌شود.
			// --------------------------------------------------------------
			options.AddPolicy(AccountSensitivePolicy, httpContext =>
				RateLimitPartition.GetFixedWindowLimiter(
					GetPartitionKey(httpContext),
					_ => new FixedWindowRateLimiterOptions
					{
						PermitLimit = AccountSensitiveSettings.Limit,
						Window = AccountSensitiveSettings.Window,
						QueueLimit = 0
					}));

			// --------------------------------------------------------------
			// "account-recovery" — ۵ در ۱۵ دقیقه — به ازای هر IP
			// برای ForgotPassword / ResetPassword / ConfirmEmail که [AllowAnonymous] هستند
			// و کاربر شناسه‌ای ندارد، پس partition بر اساس IP است.
			// --------------------------------------------------------------
			options.AddPolicy(AccountRecoveryPolicy, httpContext =>
				RateLimitPartition.GetFixedWindowLimiter(
					GetClientIp(httpContext),
					_ => new FixedWindowRateLimiterOptions
					{
						PermitLimit = AccountRecoverySettings.Limit,
						Window = AccountRecoverySettings.Window,
						QueueLimit = 0
					}));

			// --------------------------------------------------------------
			// رفتار Reject: 429 + هدرهای X-RateLimit-*
			// --------------------------------------------------------------
			options.OnRejected = async (context, cancellationToken) =>
			{
				var httpContext = context.HttpContext;
				var response = httpContext.Response;
				response.StatusCode = StatusCodes.Status429TooManyRequests;

				// تنظمیات همان Policy ای که درخواست را Reject کرده — منبعِ ساخت هدرها
				var settings = GetEffectiveSettings(httpContext);

				// X-RateLimit-Limit: سقف همان Policy
				response.Headers["X-RateLimit-Limit"] = settings.Limit.ToString();

				// X-RateLimit-Remaining: در لحظه‌ی Reject ظرفیتی باقی نمانده
				response.Headers["X-RateLimit-Remaining"] = "0";

				// Retry-After: اگر Lease خودش مقدار بدهد (نام well-known موجود در API) همان؛
				// وگرنه کل Window آن Policy (مقدار محافظه‌کارانه)
				TimeSpan retryAfter = settings.Window;
				if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var leaseRetryAfter)
					&& leaseRetryAfter > TimeSpan.Zero)
				{
					retryAfter = leaseRetryAfter;
				}
				response.Headers["Retry-After"] = ((int)retryAfter.TotalSeconds).ToString();

				// بدنه به فرمت ProblemDetails — با GlobalExceptionHandler شما هم‌خوان است
				await Results.Problem(
					statusCode: StatusCodes.Status429TooManyRequests,
					title: "Too Many Requests",
					detail: "You have exceeded the allowed request limit. Please try again later.")
					.ExecuteAsync(httpContext);
			};
		});

		return services;
	}

	/// <summary>Partition Key: UserId برای درخواست‌های احراز هویت شده، IP برای بقیه.</summary>
	private static string GetPartitionKey(HttpContext httpContext)
	{
		// توکن‌های شما ClaimTypes.NameIdentifier دارند (همان‌طور که Logout استفاده می‌کند)
		var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
					 ?? httpContext.User.FindFirstValue("sub");

		return string.IsNullOrEmpty(userId)
			? $"ip:{GetClientIp(httpContext)}"
			: $"user:{userId}";
	}

	private static string GetClientIp(HttpContext httpContext)
	{
		var ip = httpContext.Connection.RemoteIpAddress;

		// نرمال‌سازی IPv4-mapped IPv6 تا یک کلاینت همیشه در همان Partition بیفتد
		if (ip is not null && ip.IsIPv4MappedToIPv6)
			ip = ip.MapToIPv4();

		return ip?.ToString() ?? "unknown";
	}

	/// <summary>تنظمیات Policy ای که درخواست را Reject کرده — برای Fallback هدرها.</summary>
	private static (int Limit, TimeSpan Window) GetEffectiveSettings(HttpContext httpContext)
	{
		var policyName = httpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;

		return policyName switch
		{
			LoginPolicy => LoginSettings,
			SearchPolicy => SearchSettings,
			AuthPolicy => AuthSettings,
			AccountSensitivePolicy => AccountSensitiveSettings,
			AccountRecoveryPolicy => AccountRecoverySettings,
			_ => GlobalSettings
		};
	}
}
