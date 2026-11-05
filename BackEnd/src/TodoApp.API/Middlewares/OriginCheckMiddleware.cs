using Microsoft.Extensions.Options;
using TodoApp.Business.Auth;

namespace TodoApp.API.Middlewares;

/// <summary>
/// سیاست ضد-CSRF مکمل (اختیاری ولی توصیه‌شده): برای متدهای unsafe، اگر هدر Origin ارسال شده باشد
/// باید جزو originهای مجاز (فهرست فرانت + origin خودِ سرور) باشد؛ وگرنه 403.
/// کوکی‌های فعلی شما SameSite=Strict و بدنه‌ها JSON هستند؛ این میان‌افزار لایه دفاعی دوم است.
/// ثبت: app.UseMiddleware&lt;OriginCheckMiddleware&gt;(); بعد از UseAuthentication (فقط در حالت فعال‌کردن).
/// </summary>
public class OriginCheckMiddleware
{
	private readonly RequestDelegate _next;

	public OriginCheckMiddleware(RequestDelegate next)
	{
		_next = next;
	}

	public async Task InvokeAsync(HttpContext context, IOptions<SecuritySettings> securityOptions)
	{
		if (IsUnsafeMethod(context.Request.Method))
		{
			var origin = context.Request.Headers.Origin.ToString();

			if (!string.IsNullOrEmpty(origin))
			{
				var allowed = securityOptions.Value.AllowedRedirectOrigins
					.Select(o => o.Trim().TrimEnd('/'))
					.ToHashSet(StringComparer.OrdinalIgnoreCase);

				// origin خودِ سرور (همان‌مبدأ) همیشه مجاز است
				var self = $"{context.Request.Scheme}://{context.Request.Host}";
				allowed.Add(self.TrimEnd('/'));

				if (!allowed.Contains(origin.TrimEnd('/')))
				{
					context.Response.StatusCode = StatusCodes.Status403Forbidden;
					await context.Response.WriteAsJsonAsync(new
					{
						title = "Forbidden",
						detail = "درخواست از مبدأ غیرمجاز ارسال شده است."
					});
					return;
				}
			}
		}

		await _next(context);
	}

	private static bool IsUnsafeMethod(string method)
		=> HttpMethods.IsPost(method) || HttpMethods.IsPut(method)
		|| HttpMethods.IsPatch(method) || HttpMethods.IsDelete(method);
}
