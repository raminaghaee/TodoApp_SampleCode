using System.Text.Json.Serialization;

namespace TodoApp.Business.Auth;

/// <summary>POST /api/v1/auth/reset-password — بازنشانی رمز با توکن بازیابی؛ رمز فعلی گرفته نمی‌شود.</summary>
public sealed class ResetPasswordDto
{
	[JsonPropertyName("token")]
	public string Token { get; set; } = string.Empty;

	[JsonPropertyName("new_password")]
	public string NewPassword { get; set; } = string.Empty;
}
