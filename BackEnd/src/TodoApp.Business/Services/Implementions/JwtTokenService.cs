using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TodoApp.Business.Auth;
using TodoApp.Business.Entites;

namespace TodoApp.Business.Authentication;

public class JwtTokenService : IJwtTokenService
{
	private readonly JwtSettings _settings;
	/// <summary>T-024: با JwtBearerRevocationExtensions هماهنگ نگه داشته شود</summary>
	public const string JwtVersionClaimName = "jwtv";

	public JwtTokenService(IOptions<JwtSettings> options)
	{
		_settings = options.Value;
	}

	public (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(AppUser user)
	{
		var now = DateTime.UtcNow;

		var claims = new List<Claim>
		{
            // با mapping پیش‌فرض JwtBearer، claim "sub" به ClaimTypes.NameIdentifier تبدیل می‌شود
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
			new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
			new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            // T-024: نام سفارشی است؛ mapping پیش‌فرض آن را تغییر نمی‌دهد
            new(JwtVersionClaimName, user.JwtVersion.ToString()),
		};

		var credentials = new SigningCredentials(
			new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
			SecurityAlgorithms.HmacSha256);

		var expiresAtUtc = now.AddMinutes(_settings.AccessTokenExpirationMinutes);

		var token = new JwtSecurityToken(
			issuer: _settings.Issuer,
			audience: _settings.Audience,
			claims: claims,
			notBefore: now,
			expires: expiresAtUtc,
			signingCredentials: credentials);

		return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
	}
}
