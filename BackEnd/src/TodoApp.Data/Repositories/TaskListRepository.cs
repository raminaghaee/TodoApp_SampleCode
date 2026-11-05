using Microsoft.EntityFrameworkCore;
using TodoApp.Business.Entites;
using TodoApp.Business.IRepositories;
using TodoApp.Data.Contexts;

namespace TodoApp.Data.Repositories;

public class TaskListRepository : RepositoryBase<TaskList>, ITaskListRepository
{
	#region Ctors
	public TaskListRepository(TodoAppDbContext Db) : base(Db) { }
	#endregion
	#region Manipulate
	/// <summary>
	///  شناسه لیست‌های تسک های قابل ‌مشاهده برای کاربر  => مالک/اشتراک گذاری شده.
	/// </summary>
	public Task<List<int>> GetAccessibleTaskListIdsAsync(int userId, CancellationToken ct = default)
	{
		return Table().Where(
			l => l.OwnerId == userId || l.TaskListShares.Any(s => s.UserId == userId))
			.AsNoTracking()
			.Select(l => l.Id)
			.Distinct()
			.ToListAsync(ct);
	}

	/// <summary>
	///آیا به لیست تسک ها دسترسی دارد؟ => مالک/اشتراک گذاری شده
	/// </summary>
	public Task<bool> HasTaskListAccessAsync(int userId, int listId, CancellationToken ct = default)
		=> Table().AnyAsync(l => l.Id == listId
			&& (l.OwnerId == userId || l.TaskListShares.Any(s => s.UserId == userId)), ct); 
	#endregion
}
