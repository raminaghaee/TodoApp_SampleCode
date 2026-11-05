using System.Text.Json.Serialization;

namespace TodoApp.Business.TaskLists;

public sealed class TaskListShareDto
{
	[JsonPropertyName("id")]
	public int Id { get; set; }

	[JsonPropertyName("list_id")]
	public int ListId { get; set; }

	[JsonPropertyName("user_id")]
	public int UserId { get; set; }

	[JsonPropertyName("created_at")]
	public DateTime CreatedAt { get; set; }
}
