namespace TodoApp.API.Extensions;

/// <summary>
/// پیکربندی CORS (Cross-Origin Resource Sharing).
/// کنترل می‌کند کدام دامنه‌ها می‌توانند به API دسترسی داشته باشند.
/// </summary>
public static class CorsExtensions
{
	/// <summary>
	/// نام Policy پیش‌فرض — سراسری برای کل API استفاده می‌شود.
	/// </summary>
	public const string DefaultPolicyName = "Web";

	/// <summary>
	/// سرویس‌های CORS را با Policy «Web» ثبت می‌کند.
	/// Origins از بخش «Cors:AllowedOrigins» در appsettings.json خوانده می‌شود.
	/// </summary>
	/// <remarks>
	/// نکات امنیتی:
	/// • از WithOrigins استفاده می‌کنیم (نه AllowAnyOrigin) چون AllowCredentials فعال است.
	///   AllowAnyOrigin + AllowCredentials توسط Spec CORS ممنوع است و مرورگر درخواست را رد می‌کند.
	/// • AllowCredentials برای ارسال HttpOnly Cookie (Access/Refresh Token) ضروری است.
	/// • AllowAnyHeader و AllowAnyMethod برای انعطاف‌پذیری فرانت‌اند.
	/// </remarks>
	public static IServiceCollection AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
	{
		var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins")
			.Get<string[]>() ?? Array.Empty<string>();

		if (allowedOrigins.Length == 0)
			throw new InvalidOperationException(
				"هیچ Origin مجازی در «Cors:AllowedOrigins» تنظیم نشده است. " +
				"حداقل یک Origin (مثل http://localhost:3000) لازم است چون AllowCredentials فعال است.");

		// اعتبارسنجی ساده فرمت Origin (scheme://host[:port]) — جلوگیری از اشتباه تایپی مثل نداشتن http/https
		foreach (var origin in allowedOrigins)
		{
			if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
				|| (uri.Scheme != "http" && uri.Scheme != "https"))
			{
				throw new InvalidOperationException(
					$"Origin نامعتبر در «Cors:AllowedOrigins»: «{origin}». " +
					"باید فرمت کامل داشته باشد مثل http://localhost:3000 یا https://todo.example.com.");
			}
		}

		services.AddCors(options =>
		{
			options.AddPolicy(DefaultPolicyName, policy => policy
				.WithOrigins(allowedOrigins)   // ← صریح، نه AllowAnyOrigin
				.AllowAnyHeader()
				.AllowAnyMethod()
				.AllowCredentials());          // ← برای Cookie
		});

		return services;
	}

	/// <summary>
	/// Middleware UseCors را با Policy پیش‌فرض در Pipeline فعال می‌کند.
	/// </summary>
	/// <remarks>
	/// ترتیب حیاتی است: UseCors باید بعد از UseRouting و قبل از UseAuthentication / UseAuthorization باشد.
	/// </remarks>
	public static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app)
		=> app.UseCors(DefaultPolicyName);
}
