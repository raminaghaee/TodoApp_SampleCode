using System.Text.Json.Serialization;

namespace TodoApp.Business.TaskItems;

public sealed class CreateTaskItemRequest
{
	[JsonPropertyName("list_id")]
	public int TaskListId { get; set; }

	[JsonPropertyName("title")]
	public string Title { get; set; } = string.Empty;

	[JsonPropertyName("is_my_day")]
	public bool? IsMyDay { get; set; }

	[JsonPropertyName("is_important")]
	public bool? IsImportant { get; set; }

	[JsonPropertyName("due_date")]
	public DateOnly? DueDate { get; set; }

	[JsonPropertyName("assigned_to")]
	public int? AssignedTo { get; set; }
}
