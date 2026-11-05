namespace TodoApp.Business.Entites;

public class TaskItem : BaseEntity
{
	#region Props
	public int TaskListId { get; set; }
	public string Title { get; set; } = string.Empty;
	/// <summary>ايجاد كننده تسك → Users.Id</summary>
	public int? AssignedToId { get; set; }
	public string? Notes { get; set; }
	public int CreatedById { get; set; }
	/// <summary>مسئول انجام تسك → Users.Id — nullable</summary>
	public bool IsCompleted { get; set; }
	/// <summary>مهم/ستاره</summary>
	public bool IsImportant { get; set; }
	public bool IsMyDay { get; set; }
	/// <summary>سررسید — فقط تاریخ (فرمت YYYY-MM-DD در JSON)</summary>
	public DateOnly? DueDate { get; set; }
	public DateTime? CompletedAt { get; set; }

	/// <summary>
	/// T-011: Optimistic Concurrency Token — توسط SQL Server در هر Update خودکار افزایش می‌یابد.
	/// </summary>
	public byte[] RowVersion { get; set; } = [];

	/// <summary>
	/// T-025: اگر true باشد، ReminderJob برای نزدیک‌شدن DueDate رکورد Reminder می‌سازد.
	/// </summary>
	public bool ReminderEnabled { get; set; } = true;
	#endregion
	#region Navigations
	public TaskList TaskList { get; set; } 
	#endregion
}
