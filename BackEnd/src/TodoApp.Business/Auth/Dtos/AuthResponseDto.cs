namespace TodoApp.Business.Auth;

/// <summary>
/// خروجی سرویس‌های Auth (ثبت‌نام / ورود).
///  توکن‌ها فقط برای تنظیم HttpOnly Cookie استفاده می‌شوند و Controller
/// آن‌ها را در Body پاسخ برنمی‌گرداند (طبق یادداشت فنی تسک).
/// </summary>
public sealed record AuthResponseDto
{
	public required UserDto User { get; init; }
	public required string AccessToken { get; init; }
	public required string RefreshToken { get; init; }
	public required DateTimeOffset AccessTokenExpiresAt { get; init; }
	public required DateTimeOffset RefreshTokenExpiresAt { get; init; }
}
