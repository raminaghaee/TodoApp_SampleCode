using TodoApp.Business.Entites;

namespace TodoApp.Business.IRepositories;

public interface ITaskListRepository : IRepositoryBase<TaskList>
{
	/// <summary>
	///  شناسه لیست‌های تسک های قابل ‌مشاهده برای کاربر  => مالک/اشتراک گذاری شده.
	/// </summary>
	Task<List<int>> GetAccessibleTaskListIdsAsync(int userId, CancellationToken ct = default);
	/// <summary>
	///آیا به لیست تسک ها دسترسی دارد؟ => مالک/اشتراک گذاری شده
	/// </summary>
	Task<bool> HasTaskListAccessAsync(int userId, int listId, CancellationToken ct = default);
}
