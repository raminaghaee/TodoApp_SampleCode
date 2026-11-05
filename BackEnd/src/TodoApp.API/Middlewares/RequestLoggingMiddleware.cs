using System.Diagnostics;
using Serilog;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace TodoApp.API.Middlewares;

/// <summary>
/// برای هر درخواست HTTP یک لاگ ساختاریافته ثبت می‌کند:
/// متد، مسیر، کد وضعیت و زمان اجرا (Elapsed) — برای تشخیص Bottleneck و خطاها.
/// </summary>
public sealed class RequestLoggingMiddleware
{
	private const string MessageTemplate =
		"HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0} ms";

	/// <summary>کلیدهای Query String حساس که نباید در لاگ ظاهر شوند.</summary>
	private static readonly HashSet<string> SensitiveQueryKeys = new(StringComparer.OrdinalIgnoreCase)
	{
		"password", "passwd", "token", "access_token", "refresh_token",
		"api_key", "apikey", "secret", "authorization"
	};

	private readonly RequestDelegate _next;
	private readonly ILogger _logger;

	public RequestLoggingMiddleware(RequestDelegate next)
	{
		_next = next ?? throw new ArgumentNullException(nameof(next));
		_logger = Log.ForContext<RequestLoggingMiddleware>();
	}

	public async Task InvokeAsync(HttpContext context)
	{
		ArgumentNullException.ThrowIfNull(context);

		var stopwatch = Stopwatch.StartNew();
		var path = context.Request.Path.ToString();
		var query = SanitizeQueryString(context.Request.Query);

		try
		{
			await _next(context);

			stopwatch.Stop();
			Write(context.Request.Method, path, query,
				  context.Response.StatusCode,
				  stopwatch.Elapsed.TotalMilliseconds);
		}
		catch (Exception ex)
		{
			stopwatch.Stop();
			Write(context.Request.Method, path, query,
				  StatusCodes.Status500InternalServerError,
				  stopwatch.Elapsed.TotalMilliseconds, ex);
			throw; // برای Exception Handling Middleware دوباره پرتاب می‌شود
		}
	}

	private void Write(string method, string path, string? query,
					   int statusCode, double elapsedMs, Exception? ex = null)
	{
		var fullPath = string.IsNullOrEmpty(query) ? path : $"{path}?{query}";

		var level = ex is not null
			? LogEventLevel.Error
			: statusCode >= 500 ? LogEventLevel.Error
			: statusCode >= 400 ? LogEventLevel.Warning
			: LogEventLevel.Information;

		// هر فیلد به‌صورت Property جداگانه در JSON ذخیره می‌شود
		_logger.Write(level, ex, MessageTemplate, method, fullPath, statusCode, elapsedMs);
	}

	/// <summary>مقدار کلیدهای حساس (توکن/پسورد) را قبل از لاگ Mask می‌کند.</summary>
	private static string? SanitizeQueryString(IQueryCollection query)
	{
		return query.Count == 0
			? null
			: string.Join("&", query.SelectMany(pair =>
				pair.Value.Select(value =>
					SensitiveQueryKeys.Contains(pair.Key)
						? $"{pair.Key}=***MASKED***"
						: $"{pair.Key}={Uri.EscapeDataString(value)}")));
	}
}
