using System.Text.Json.Serialization;

namespace TodoApp.Business.Auth;

public sealed record RegisterDto
{
	/// <summary>ایمیل کاربر؛ یکتا و Case-Insensitive (مقایسه از طریق NormalizedEmail انجام می‌شود)</summary>
	[JsonPropertyName("email")]
	public string Email { get; init; } = string.Empty;

	/// <summary>رمز عبور؛ حداقل ۸ کاراکتر شامل حرف بزرگ، حرف کوچک و عدد</summary>
	[JsonPropertyName("password")]
	public string Password { get; init; } = string.Empty;

	[JsonPropertyName("display_name")]
	public string? DisplayName { get; set; }
}
