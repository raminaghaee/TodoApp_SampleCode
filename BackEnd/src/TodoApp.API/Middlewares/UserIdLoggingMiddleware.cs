using System.Security.Claims;
using Serilog.Context;

namespace TodoApp.API.Middlewares;

/// <summary>
/// پس از احراز هویت، UserId و RequestId را به LogContext اضافه می‌کند
/// تا «تمام» لاگ‌های صادرشده در همین Scope درخواست شامل این فیلدها باشند
/// و لاگ‌های مربوط به یک کاربر قابل پیگیری (Correlate) باشند.
/// </summary>
public sealed class UserIdLoggingMiddleware
{
	public const string UserIdProperty = "UserId";
	public const string RequestIdProperty = "RequestId";

	private readonly RequestDelegate _next;

	public UserIdLoggingMiddleware(RequestDelegate next)
	{
		_next = next ?? throw new ArgumentNullException(nameof(next));
	}

	public async Task InvokeAsync(HttpContext context)
	{
		// پشتیبانی از Claim استاندارد JWT (sub) و ClaimTypes.NameIdentifier
		var userId =
			context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
			context.User?.FindFirstValue("sub") ??
			"anonymous";

		// using تضمین می‌کند Context بعد از پایان درخواست Dispose شود
		using (LogContext.PushProperty(RequestIdProperty, context.TraceIdentifier))
		using (LogContext.PushProperty(UserIdProperty, userId))
		{
			await _next(context);
		}
	}
}
