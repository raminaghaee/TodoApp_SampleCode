namespace TodoApp.Business;

public interface ISoftDeletable
{
	/// <summary>آیا رکورد حذف منطقی شده است؟</summary>
	bool IsDeleted { get; set; }

	/// <summary>زمان حذف منطقی (UTC) — null یعنی حذف نشده</summary>
	DateTime? DeletedAt { get; set; }
}
