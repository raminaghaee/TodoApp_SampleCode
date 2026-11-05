namespace TodoApp.Business.Entites;

public class TaskList : BaseEntity
{
	#region Props
	/// <summary>مالک لیست → Users.Id</summary>
	public int OwnerId { get; set; }
	public string Name { get; set; } = string.Empty;
	/// <summary>یکی از: blue, teal, violet, amber, rose</summary>
	public string Color { get; set; } = "blue";
	#endregion
	#region Navigations
	public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
	public ICollection<TaskListShare> TaskListShares { get; set; } = new List<TaskListShare>(); 
	#endregion
}
