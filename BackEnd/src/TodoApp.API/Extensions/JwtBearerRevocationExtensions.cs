using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using TodoApp.Business.Authentication;
using TodoApp.Business.Entites;

namespace TodoApp.API.Extensions;

public static class JwtBearerRevocationExtensions
{
	/// <summary>هماهنگ با AuthController.AccessTokenCookieName</summary>
	public const string AccessTokenCookieName = "access_token";

	public static JwtBearerOptions EnableTokenRevocation(this JwtBearerOptions options)
	{
		options.Events ??= new JwtBearerEvents();

		// ─── ۱) خواندن توکن از HttpOnly کوکی (JwtBearer فقط هدر را می‌خواند) ───
		var innerOnMessageReceived = options.Events.OnMessageReceived;
		options.Events.OnMessageReceived = context =>
		{
			if (innerOnMessageReceived is not null)
				innerOnMessageReceived(context);

			// هدر اولویت دارد؛ اگر نبود، از کوکی بخوان
			if (string.IsNullOrEmpty(context.Token))
			{
				var cookieToken = context.Request.Cookies[AccessTokenCookieName];
				if (!string.IsNullOrEmpty(cookieToken))
					context.Token = cookieToken;
			}

			return Task.CompletedTask;
		};

		// ─── ۲) ابطال با JwtVersion — مثل قبل ───
		var innerOnTokenValidated = options.Events.OnTokenValidated;
		options.Events.OnTokenValidated = async context =>
		{
			if (innerOnTokenValidated is not null)
				await innerOnTokenValidated(context);

			var principal = context.Principal;

			// هر دو حالت: با mapping فعال → NameIdentifier | غیرفعال → sub
			var idValue = principal?.FindFirstValue(ClaimTypes.NameIdentifier)
					   ?? principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);

			if (!int.TryParse(idValue, out var userId))
			{
				context.Fail("توکن فاقد شناسه کاربر معتبر است.");
				return;
			}

			var userManager = context.HttpContext.RequestServices
				.GetRequiredService<UserManager<AppUser>>();

			var user = await userManager.FindByIdAsync(userId.ToString());
			if (user is null)
			{
				context.Fail("کاربر یافت نشد.");
				return;
			}

			if (!int.TryParse(principal!.FindFirstValue(JwtTokenService.JwtVersionClaimName), out var tokenVersion)
				|| tokenVersion != user.JwtVersion)
			{
				context.Fail("توکن باطل شده است.");
				return;
			}
		};

		return options;
	}
}
