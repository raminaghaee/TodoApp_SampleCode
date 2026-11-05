using System.Text.Json.Serialization;

namespace TodoApp.Business.Notifications;

public sealed class NotificationDto
{
	[JsonPropertyName("id")]
	public int Id { get; set; }

	[JsonPropertyName("user_id")]
	public int UserId { get; set; }

	[JsonPropertyName("type")]
	public string Type { get; set; } = "info";

	[JsonPropertyName("title")]
	public string Title { get; set; } = string.Empty;

	[JsonPropertyName("body")]
	public string? Body { get; set; }

	[JsonPropertyName("task_id")]
	public int? TaskItemId { get; set; }

	[JsonPropertyName("read_at")]
	public DateTime? ReadAt { get; set; }

	[JsonPropertyName("created_at")]
	public DateTime CreatedAt { get; set; }
}
