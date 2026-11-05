using System.Text.Json.Serialization;

namespace TodoApp.Business.Auth;

/// <summary>POST /api/v1/auth/change-email — درخواست تغییر ایمیل (نیازمند نشست فعال).</summary>
public sealed class ChangeEmailDto
{
	[JsonPropertyName("new_email")]
	public string NewEmail { get; set; } = string.Empty;

	[JsonPropertyName("current_password")]
	public string CurrentPassword { get; set; } = string.Empty;

	/// <summary>آدرس صفحه confirm-email فرانت؛ origin/path آن با فهرست مجاز سمت سرور تطبیق داده می‌شود.</summary>
	[JsonPropertyName("redirect_url")]
	public string RedirectUrl { get; set; } = string.Empty;
}
