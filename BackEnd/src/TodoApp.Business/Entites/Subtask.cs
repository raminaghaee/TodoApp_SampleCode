using TodoApp.Business.Common;

namespace TodoApp.Business.Entites;

public class Subtask : BaseEntity,ICurrentUserService
{
	#region Props
	public int TaskItemId { get; set; }

	public string Title { get; set; } = string.Empty;

	public bool IsCompleted { get; set; }

	/// <summary>ترتیب نمایش</summary>
	public int Position { get; set; }

	public int UserId { get; set; } 
	#endregion
}
