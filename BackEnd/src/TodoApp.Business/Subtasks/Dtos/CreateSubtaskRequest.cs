using System.Text.Json.Serialization;

namespace TodoApp.Business.Subtasks;

public sealed class CreateSubtaskRequest
{
	[JsonPropertyName("title")]
	public string Title { get; set; } = string.Empty;

	/// <summary>
	///  اختیاری؛ اگر ارسال نشود در انتهای فهرست قرار می‌گیرد
	/// </summary>
	[JsonPropertyName("position")]
	public int? Position { get; set; }
}
