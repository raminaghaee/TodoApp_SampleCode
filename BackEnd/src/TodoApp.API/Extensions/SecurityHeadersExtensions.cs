namespace TodoApp.API.Extensions;

/// <summary>
/// پیکربندی Security Headers Middleware — T-028 (NFR-02)
/// شامل: X-Frame-Options, X-Content-Type-Options, Referrer-Policy, CSP
/// (HSTS جداگانه و فقط در Production با UseHsts فعال می‌شود)
/// </summary>
public static class SecurityHeadersExtensions
{
	public static IServiceCollection AddSecurityHeaders(this IServiceCollection services)
	{
		services.AddSecurityHeaderPolicies()
			.SetDefaultPolicy(CreatePolicy());  

		return services;
	}

	private static HeaderPolicyCollection CreatePolicy() =>
		new HeaderPolicyCollection()
			// جلوگیری از نمایش در iframe (حمله Clickjacking)
			.AddFrameOptionsDeny()
			// جلوگیری از MIME Sniffing
			.AddContentTypeOptionsNoSniff()
			// محدودسازی ارسال Referrer
			.AddReferrerPolicyStrictOriginWhenCrossOrigin()
			// کاهش ریسک XSS — فقط منابع Same-Origin
			.AddContentSecurityPolicy(builder =>
			{
				builder.AddDefaultSrc().Self();
			});
}
