namespace TodoApp.Business.Entites;

public class TaskListShare : BaseEntity
{
	#region Props
	public int TaskListId { get; set; }

	/// <summary>کاربری که لیست با او به اشتراک گذاشته شده → Users.Id</summary>
	public int UserId { get; set; }
	#endregion
	#region Navigations
	public TaskList TaskList { get; set; }
	public AppUser AppUser { get; set; } 
	#endregion
}
