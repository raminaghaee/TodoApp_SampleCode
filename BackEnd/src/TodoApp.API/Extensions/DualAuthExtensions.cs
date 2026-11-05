using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace TodoApp.API.Extensions;


/// <summary>
/// احراز هویت دوگانه (تصمیم پروژه):
/// • فرانت قراردادی: توکن JWT در بدنه پاسخ برمی‌گردد و کلاینت با «Authorization: Bearer» می‌فرستد.
/// • جریان فعلی v1: همان کوکی‌های access_token/refresh_token سر جای خود کار می‌کنند.
/// روش کار: یک PolicyScheme تصمیم می‌گیرد درخواست با Bearer اعتبارسنجی شود یا با کوکی.
/// </summary>
public static class DualAuthExtensions
{
	public const string DualScheme = "DualAuth";

	public static IServiceCollection AddDualAuthentication(this IServiceCollection services, IConfiguration configuration)
	{
		//  مقادیر باید دقیقاً با همان Key/Issuer/Audience که در AuthService توکن می‌سازید یکی باشد.
		var jwtKey = configuration["Jwt:Key"] ?? configuration["Jwt:Secret"] ?? configuration["Jwt:SecretKey"];
		var issuer = configuration["Jwt:Issuer"];
		var audience = configuration["Jwt:Audience"];

		if (string.IsNullOrWhiteSpace(jwtKey))
			throw new InvalidOperationException("کلید JWT یافت نشد — بخش «Jwt:Key» را در appsettings تنظیم کنید.");

		var tokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
			ValidIssuer = issuer,
			ValidateAudience = !string.IsNullOrWhiteSpace(audience),
			ValidAudience = audience,
			ValidateIssuerSigningKey = true,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
			ValidateLifetime = true,
			ClockSkew = TimeSpan.FromMinutes(1),
			// نام کاربری را روی claim شناسه بگذاریم (سازگار با ClaimTypes.NameIdentifier)
			NameClaimType = ClaimTypes.NameIdentifier
		};

		services.AddAuthentication(DualScheme)
				.AddPolicyScheme(DualScheme, "Bearer یا Cookie", options =>
				{
					options.ForwardDefaultSelector = context =>
									context.Request.Headers.ContainsKey("Authorization")
											? JwtBearerDefaults.AuthenticationScheme
											: CookieAuthenticationDefaults.AuthenticationScheme;
				})
				.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
				{
					options.TokenValidationParameters = tokenValidationParameters;
					options.Events = new JwtBearerEvents
					{
						// قرارداد فرانت: 401 با بدنه JSON { message } — کلاینت روی 401 سشن را پاک می‌کند
						OnChallenge = async context =>
						{
							context.HandleResponse();
							context.Response.StatusCode = StatusCodes.Status401Unauthorized;
							context.Response.ContentType = "application/json; charset=utf-8";
							await context.Response.WriteAsJsonAsync(new { message = "احراز هویت لازم است یا توکن منقضی شده است." });
						}
					};
				})
				.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
				{
					// همان کوکی‌ای که v1 ست می‌کند — مقدار آن JWT خام است
					options.Cookie.Name = "access_token";
					options.Cookie.HttpOnly = true;
					options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
					options.Cookie.SameSite = SameSiteMode.Strict;
					options.TicketDataFormat = new JwtCookieTicketDataFormat(tokenValidationParameters);
				});

		return services;
	}
}

/// <summary>کوکی access_token محتوای JWT خام دارد؛ این فرمت آن را به Ticket تبدیل می‌کند.</summary>
internal sealed class JwtCookieTicketDataFormat : ISecureDataFormat<AuthenticationTicket>
{
	private readonly TokenValidationParameters _tokenValidationParameters;

	public JwtCookieTicketDataFormat(TokenValidationParameters tokenValidationParameters)
			=> _tokenValidationParameters = tokenValidationParameters;

	public AuthenticationTicket? Unprotect(string? protectedText, string? purpose)
	{
		if (string.IsNullOrWhiteSpace(protectedText))
			return null;

		var handler = new JwtSecurityTokenHandler();
		var principal = handler.ValidateToken(protectedText, _tokenValidationParameters, out _);
		return new AuthenticationTicket(principal, CookieAuthenticationDefaults.AuthenticationScheme);
	}

	public AuthenticationTicket? Unprotect(string? protectedText) =>
			Unprotect(protectedText, purpose: null);

	public string Protect(AuthenticationTicket data, string? purpose) =>
			throw new NotSupportedException("صدور کوکی از مسیر SetAuthCookies کنترلرها انجام می‌شود؛ این فرمت فقط برای خواندن است.");

	public string Protect(AuthenticationTicket data) =>
			Protect(data, purpose: null);
}
