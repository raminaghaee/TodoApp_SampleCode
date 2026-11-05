using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TodoApp.Business;

/// <summary>
/// کلاس پایه تمام Entity های سیستم — فیلدهای مشترک را یک‌بار متمرکز می‌کند.
/// </summary>
public abstract class BaseEntity : IBaseEntity, IAuditableEntity, ISoftDeletable
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	public int Id { get; set; }

	/// <summary>زمان ایجاد (UTC) — مقدار اولیه، تضمین می‌کند CreatedAt هیچ‌وقت null/default نماند</summary>
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	/// <summary>زمان آخرین ویرایش (UTC)</summary>
	public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;

	/// <summary>وضعیت حذف منطقی</summary>
	public bool IsDeleted { get; set; }

	/// <summary>زمان حذف منطقی (UTC)</summary>
	public DateTime? DeletedAt { get; set; }
}
