using System.Text.Json.Serialization;

namespace TodoApp.Business.Auth;

/// <summary>POST /api/v1/auth/change-password — تغییر رمز از پروفایل (نیازمند نشست فعال).</summary>
public sealed class ChangePasswordDto
{
	[JsonPropertyName("current_password")]
	public string CurrentPassword { get; set; } = string.Empty;

	[JsonPropertyName("new_password")]
	public string NewPassword { get; set; } = string.Empty;
}
