using System.Text.Json.Serialization;

namespace TodoApp.Business.Admin;

public sealed class AdminUserDto
{
	[JsonPropertyName("id")]
	public int Id { get; set; }

	[JsonPropertyName("email")]
	public string? Email { get; set; }

	[JsonPropertyName("display_name")]
	public string? DisplayName { get; set; }

	[JsonPropertyName("is_active")]
	public bool IsActive { get; set; }

	[JsonPropertyName("created_at")]
	public DateTime CreatedAt { get; set; }

	[JsonPropertyName("role")]
	public string Role { get; set; } = "user";
}
