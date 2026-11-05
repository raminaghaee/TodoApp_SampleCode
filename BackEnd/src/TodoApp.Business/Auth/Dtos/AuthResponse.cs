using System.Text.Json.Serialization;

namespace TodoApp.Business.Auth;

/// <summary>پاسخ ورود/ثبت‌نام قرارداد فرانت: { token, user }</summary>
public sealed class AuthResponse
{
	[JsonPropertyName("token")]
	public string Token { get; set; } = string.Empty;

	[JsonPropertyName("user")]
	public UserDto User { get; set; } = new();
}
