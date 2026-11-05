using System.Text.Json.Serialization;

namespace TodoApp.Business.Auth;

/// <summary>POST /api/v1/auth/confirm-email — تأیید ایمیل جدید با توکن.</summary>
public sealed class ConfirmEmailDto
{
	[JsonPropertyName("token")]
	public string Token { get; set; } = string.Empty;
}
