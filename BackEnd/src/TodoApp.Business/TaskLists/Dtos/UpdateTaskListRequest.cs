using System.Text.Json.Serialization;

namespace TodoApp.Business.TaskLists;

public sealed class UpdateTaskListRequest
{
	[JsonPropertyName("name")]
	public string? Name { get; set; }

	[JsonPropertyName("color")]
	public string? Color { get; set; }
}
