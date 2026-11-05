using System.Text.Json.Serialization;

namespace TodoApp.Business.Subtasks;

public sealed class UpdateSubtaskRequest
{
	[JsonPropertyName("title")]
	public string? Title { get; set; }

	[JsonPropertyName("is_completed")]
	public bool? IsCompleted { get; set; }

	[JsonPropertyName("position")]
	public int? Position { get; set; }
}
