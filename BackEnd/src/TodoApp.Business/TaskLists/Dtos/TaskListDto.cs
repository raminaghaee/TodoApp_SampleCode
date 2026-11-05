using System.Text.Json.Serialization;

namespace TodoApp.Business.TaskLists;

public sealed class TaskListDto
{
	[JsonPropertyName("id")]
	public int Id { get; set; }

	[JsonPropertyName("owner_id")]
	public int OwnerId { get; set; }

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("color")]
	public string Color { get; set; } = "blue";

	[JsonPropertyName("created_at")]
	public DateTime CreatedAt { get; set; }
}
