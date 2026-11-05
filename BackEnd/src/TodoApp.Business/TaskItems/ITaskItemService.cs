using TodoApp.Business.Entites;

namespace TodoApp.Business.TaskItems;

public interface ITaskItemService
{
	/// <summary>
	/// همه کارهای قابل‌ مشاهده کاربر -
	/// مرتب سازی نزولی بر اساس تاریخ ایجاد
	/// </summary>
	Task<IReadOnlyList<TaskItemDto>> GetVisibleAsync(int userId, CancellationToken ct = default);

	Task<TaskItemDto> CreateAsync(int userId, CreateTaskItemRequest request, CancellationToken ct = default);
	/// <summary>
	/// ویرایش بخشی از فیلدها + اعلان هنگام تغییر ارجاع
	/// </summary>
	Task<TaskItemDto> UpdateAsync(int userId, int taskId, UpdateTaskItemRequest request, CancellationToken ct = default);
	Task DeleteAsync(int userId, int taskId, CancellationToken ct = default);

	/// <summary>
	/// همه کارهای قابل‌ مشاهده کاربر
	/// مرتب سازی نزولی بر اساس تاریخ ایجاد
	/// </summary>
	bool CanModifyTaskItemAsync(int userId, TaskItem task);
}
