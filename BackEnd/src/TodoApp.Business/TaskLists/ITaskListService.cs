namespace TodoApp.Business.TaskLists;

public interface ITaskListService
{
	/// <summary>
	/// لیست‌های من = مالک + اشتراک‌شده -
	/// مرتب سازی به صورت صعودی بر اساس تاریخ ایجاد
	/// </summary>
	Task<IReadOnlyList<TaskListDto?>> GetMyTaskListsAsync(int userId, CancellationToken ct = default);
	/// <summary>
	/// همه اشتراک‌ های ، تسک لیست ، قابل‌مشاهده برای من
	/// </summary>
	Task<IReadOnlyList<TaskListShareDto>> GetVisibleTaskListSharesAsync(int userId, CancellationToken ct = default);

	Task<TaskListDto> CreateAsync(int userId, CreateTaskListRequest request, CancellationToken ct = default);

	/// <summary>
	/// ویرایش فقط توسط مالک +کاربر مشترک
	/// </summary>
	Task<TaskListDto> UpdateAsync(int userId, int listId, UpdateTaskListRequest request, CancellationToken ct = default);

	/// <summary>
	/// حذف فقط توسط مالک.
	/// </summary>
	Task DeleteAsync(int userId, int listId, CancellationToken ct = default);

	/// <summary>
	/// اشتراک‌گذاری لیست فقط توسط مالک
	/// </summary>
	Task<TaskListShareDto> CreateShareAsync(int userId, int listId, CreateTaskListShareRequest request, CancellationToken ct = default);

	/// <summary>
	/// حذف اشتراک فقط مالک
	/// </summary>
	Task DeleteShareAsync(int userId, int listId, int targetUserId, CancellationToken ct = default);
}
