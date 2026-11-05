using System.Text.Json.Serialization;

namespace TodoApp.Business.TaskLists;

public sealed class CreateTaskListShareRequest
{
	[JsonPropertyName("user_id")]
	public int UserId { get; set; }
}
