using System.Text.Json.Serialization;

namespace TodoApp.Business.TaskItems;

public sealed class UpdateTaskItemRequest
{
	[JsonPropertyName("title")]
	public string? Title { get; set; }

	[JsonPropertyName("notes")]
	public string? Notes { get; set; }

	[JsonPropertyName("is_completed")]
	public bool? IsCompleted { get; set; }

	[JsonPropertyName("is_important")]
	public bool? IsImportant { get; set; }

	[JsonPropertyName("is_my_day")]
	public bool? IsMyDay { get; set; }

	[JsonPropertyName("due_date")]
	public DateOnly? DueDate { get; set; }

	[JsonPropertyName("clear_due_date")]
	public bool? ClearDueDate { get; set; }

	[JsonPropertyName("assigned_to")]
	public int? AssignedTo { get; set; }

	[JsonPropertyName("completed_at")]
	public DateTime? CompletedAt { get; set; }

	[JsonPropertyName("list_id")]
	public int TaskListId { get; set; }
}
