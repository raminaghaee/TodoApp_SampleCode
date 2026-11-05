namespace TodoApp.Business;

public interface IAuditableEntity
{
	/// <summary>زمان ایجاد رکورد (همیشه UTC)</summary>
	DateTime CreatedAt { get; set; }

	/// <summary>زمان آخرین به‌روزرسانی — تا قبل از اولین ویرایش null است</summary>
	DateTime? UpdatedAt { get; set; }
}
