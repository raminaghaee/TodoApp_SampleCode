using System.Text.Json.Serialization;

namespace TodoApp.Business.Notifications;

public sealed class CreateNotificationRequest
{
	[JsonPropertyName("type")]
	public string Type { get; set; } = "info";

	[JsonPropertyName("title")]
	public string Title { get; set; } = string.Empty;

	[JsonPropertyName("body")]
	public string? Body { get; set; }

	[JsonPropertyName("task_id")]
	public int? TaskItemId { get; set; }
}
