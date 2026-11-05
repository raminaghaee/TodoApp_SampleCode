using System.Text.Json.Serialization;

namespace TodoApp.Business.Subtasks;

public sealed class SubtaskDto
{
	[JsonPropertyName("id")]
	public int Id { get; set; }

	[JsonPropertyName("task_id")]
	public int TaskItemId { get; set; }

	[JsonPropertyName("title")]
	public string Title { get; set; } = string.Empty;

	[JsonPropertyName("is_completed")]
	public bool IsCompleted { get; set; }

	[JsonPropertyName("position")]
	public int Position { get; set; }

	[JsonPropertyName("created_by")]
	public int CreatedBy { get; set; }

	[JsonPropertyName("created_at")]
	public DateTime CreatedAt { get; set; }

	[JsonPropertyName("updated_at")]
	public DateTime? UpdatedAt { get; set; }
}
