using TodoApp.Business.Common;

namespace TodoApp.Business.Entites;

public class Notification : BaseEntity, ICurrentUserService
{
	#region Props
	/// <summary>گیرنده → Users.Id</summary>
	public int UserId { get; set; }

	/// <summary>مثلاً: info, task_assigned, due_soon</summary>
	public string Type { get; set; } = "info";

	public string Title { get; set; } = string.Empty;

	public string? Body { get; set; }

	public int? TaskItemId { get; set; }

	public DateTime? ReadAt { get; set; } 
	#endregion
}
