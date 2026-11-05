using System.Text.Json.Serialization;

namespace TodoApp.Business.Auth;


// ─────────────────────────────────────────────────────────────────────────────
// قرارداد دقیق فرانت‌اند: نام فیلدهای JSON دقیقاً snake_case است.
// از [JsonPropertyName] استفاده شده تا ورودی/خروجی مستقل از NamingPolicy سراسری
// Program.cs همیشه با قرارداد سند تطبیق داشته باشد.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>PUT /api/v1/profiles/me — فقط نام نمایشی؛ تغییر email/role/id از این مسیر ممنوع است.</summary>
public sealed class UpdateProfileDto
{
	[JsonPropertyName("display_name")]
	public string DisplayName { get; set; } = string.Empty;
}
