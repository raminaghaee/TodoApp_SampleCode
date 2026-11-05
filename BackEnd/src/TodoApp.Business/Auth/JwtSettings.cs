namespace TodoApp.Business.Auth;

public class JwtSettings
{
	public const string SectionName = "Jwt";

	public string Issuer { get; set; } = string.Empty;

	public string Audience { get; set; } = string.Empty;

	// عمداً از appsettings.json مقداردهی نمی‌شود؛
	// Development: User Secrets | Production: Environment Variable با نام Jwt__Key
	public string Key { get; set; } = string.Empty;

	public int AccessTokenExpirationMinutes { get; set; } = 15;
}
