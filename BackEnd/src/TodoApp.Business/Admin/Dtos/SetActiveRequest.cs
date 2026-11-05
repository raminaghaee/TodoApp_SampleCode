using System.Text.Json.Serialization;

namespace TodoApp.Business.Admin;

public sealed class SetActiveRequest
{
	[JsonPropertyName("is_active")]
	public bool IsActive { get; set; }
}
