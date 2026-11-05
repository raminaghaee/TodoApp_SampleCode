using System.Text.Json.Serialization;

namespace TodoApp.Business.Auth;

/// <summary>POST /api/v1/auth/forgot-password — فراموشی رمز (ناشناس).</summary>
public sealed class ForgotPasswordDto
{
	[JsonPropertyName("email")]
	public string Email { get; set; } = string.Empty;

	/// <summary>آدرس صفحه reset-password فرانت؛ با فهرست مجاز تطبیق داده می‌شود.</summary>
	[JsonPropertyName("redirect_url")]
	public string RedirectUrl { get; set; } = string.Empty;
}
