using System.Text.Json.Serialization;

namespace TodoApp.Business.Profiles;

public sealed class ProfileDto
{
	[JsonPropertyName("id")]
	public int Id { get; set; }

	[JsonPropertyName("email")]
	public string? Email { get; set; }

	[JsonPropertyName("display_name")]
	public string? DisplayName { get; set; }
}
