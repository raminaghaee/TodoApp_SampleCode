using System.Text.Json.Serialization;

namespace TodoApp.Business.Profiles;

public sealed class UpdateProfileRequest
{
	[JsonPropertyName("email")]
	public string Email { get; set; } = string.Empty;

	[JsonPropertyName("display_name")]
	public string? DisplayName { get; set; }
}
