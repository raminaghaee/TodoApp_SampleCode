using System.Text.Json.Serialization;

namespace TodoApp.Business.TaskItemAttachments;

public sealed class TaskItemAttachmentDto
{
	[JsonPropertyName("id")]
	public int Id { get; set; }

	[JsonPropertyName("task_id")]
	public int TaskItemId { get; set; }

	[JsonPropertyName("storage_path")]
	public string StoragePath { get; set; } = string.Empty;

	[JsonPropertyName("file_name")]
	public string FileName { get; set; } = string.Empty;

	[JsonPropertyName("mime_type")]
	public string? MimeType { get; set; }

	[JsonPropertyName("size_bytes")]
	public long? SizeBytes { get; set; }

	[JsonPropertyName("uploaded_by")]
	public int UploadedBy { get; set; }

	[JsonPropertyName("created_at")]
	public DateTime CreatedAt { get; set; }
}
