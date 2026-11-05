using System.Text.Json.Serialization;

namespace TodoApp.Business.TaskLists;

public sealed class CreateTaskListRequest
{
	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;

	[JsonPropertyName("color")]
	public string Color { get; set; } = "blue";
}
