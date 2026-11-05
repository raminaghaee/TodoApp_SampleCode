using System.Text.Json.Serialization;

namespace TodoApp.Business.TaskItems;

public sealed class TaskItemDto
{
	[JsonPropertyName("id")]
	public int Id { get; set; }

	[JsonPropertyName("list_id")]
	public int ListId { get; set; }

	[JsonPropertyName("created_by")]
	public int CreatedBy { get; set; }

	[JsonPropertyName("assigned_to")]
	public int? AssignedTo { get; set; }

	[JsonPropertyName("title")]
	public string Title { get; set; } = string.Empty;

	[JsonPropertyName("notes")]
	public string? Notes { get; set; }

	[JsonPropertyName("is_completed")]
	public bool IsCompleted { get; set; }

	[JsonPropertyName("is_important")]
	public bool IsImportant { get; set; }

	[JsonPropertyName("is_my_day")]
	public bool IsMyDay { get; set; }

	/// <summary>سررسید — فرمت YYYY-MM-DD</summary>
	[JsonPropertyName("due_date")]
	public DateOnly? DueDate { get; set; }

	[JsonPropertyName("completed_at")]
	public DateTime? CompletedAt { get; set; }

	[JsonPropertyName("created_at")]
	public DateTime CreatedAt { get; set; }

	[JsonPropertyName("updated_at")]
	public DateTime? UpdatedAt { get; set; }
}
