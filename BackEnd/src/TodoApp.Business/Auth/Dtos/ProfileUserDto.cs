using System.Text.Json.Serialization;

namespace TodoApp.Business.Auth;

/// <summary>
/// قرارداد پاسخ PUT /api/v1/profiles/me — «کاربر کامل» با فیلدهای دقیق snake_case.
/// نکته: عمداً از UserDto جدا نگه داشته شده تا پاسخ‌های فعلی login/register/me تغییر casing ندهند؛
/// اگر خواستید همه‌جا یکدست باشد، در README-INTEGRATION.md گزینه «یکنواخت‌سازی UserDto» آمده است.
/// </summary>
public sealed class ProfileUserDto
{
	[JsonPropertyName("id")]
	public int Id { get; set; }

	[JsonPropertyName("email")]
	public string Email { get; set; } = string.Empty;

	[JsonPropertyName("display_name")]
	public string DisplayName { get; set; } = string.Empty;

	[JsonPropertyName("role")]
	public string Role { get; set; } = "user";
}

