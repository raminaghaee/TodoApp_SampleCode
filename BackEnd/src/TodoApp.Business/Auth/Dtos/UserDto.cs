using System.Text.Json.Serialization;

namespace TodoApp.Business.Auth;

/// <summary>اطلاعات عمومی کاربر (بدون هیچ داده حساس)</summary>
public sealed record UserDto
{
	[JsonPropertyName("id")]
	public int Id { get; init; }

	[JsonPropertyName("email")]
	public string Email { get; init; } = string.Empty;

	[JsonPropertyName("display_name")]
	public string? DisplayName { get; set; }

	/// <summary>"super_admin" یا "user" — برای نمایش لینک پنل سوپرادمین در فرانت</summary>
	[JsonPropertyName("role")]
	public string Role { get; set; } = "user";
}
