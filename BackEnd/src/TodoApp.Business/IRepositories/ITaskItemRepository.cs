using TodoApp.Business.Entites;

namespace TodoApp.Business.IRepositories;

public interface ITaskItemRepository : IRepositoryBase<TaskItem>
{
	/// <summary>
	/// همه کارهای قابل‌ مشاهده کاربر
	/// مرتب سازی نزولی بر اساس تاریخ ایجاد
	/// </summary>
	Task<IReadOnlyList<TaskItem>> GetVisibleTaskItemAsync(int userId, CancellationToken ct = default);

	/// <summary>
	/// واکشی تسک با کنترل دسترسی/
	/// یا مالک لیست باشه یا تسک بهش ارجاع شده یا لیست تسک ها باهاش اشتراک گذاری شده
	/// </summary>
	Task<TaskItem?> GetTaskItemWithAccessAsync(int userId, int taskId, CancellationToken ct = default);

	/// <summary>
	/// کنترل دسترسی کاربر به تسک/
	/// یا مالک لیست باشه یا تسک بهش ارجاع شده یا لیست تسک ها باهاش اشتراک گذاری شده
	/// </summary>
	Task<bool> HasTaskItemAccessAsync(int userId, int taskId, CancellationToken ct = default);
}
