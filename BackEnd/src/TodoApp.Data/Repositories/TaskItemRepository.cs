using Microsoft.EntityFrameworkCore;
using TodoApp.Business.Entites;
using TodoApp.Business.IRepositories;
using TodoApp.Data.Contexts;

namespace TodoApp.Data.Repositories;

public class TaskItemRepository : RepositoryBase<TaskItem>, ITaskItemRepository
{
	#region fields
	private readonly ITaskListRepository _taskListRepository; 
	#endregion
	#region Ctors
	public TaskItemRepository(TodoAppDbContext Db, ITaskListRepository taskListRepository) : base(Db)
	{
		_taskListRepository = taskListRepository;
	}
	#endregion
	#region Manipulate
	/// <summary>
	/// همه کارهای قابل‌ مشاهده کاربر
	/// مرتب سازی نزولی بر اساس تاریخ ایجاد
	/// </summary>
	public async Task<IReadOnlyList<TaskItem>> GetVisibleTaskItemAsync(int userId, CancellationToken ct = default)
	{
		var taskListIds = await _taskListRepository.GetAccessibleTaskListIdsAsync(userId, ct);
		return await Table().Where(
				t => taskListIds.Contains(t.TaskListId) || t.AssignedToId == userId)
			.AsNoTracking()
			.OrderByDescending(t => t.CreatedAt)
			.ToListAsync(ct);
	}

	/// <summary>
	/// واکشی تسک با کنترل دسترسی/
	/// یا مالک لیست باشه یا تسک بهش ارجاع شده یا لیست تسک ها باهاش اشتراک گذاری شده
	/// </summary>
	public Task<TaskItem?> GetTaskItemWithAccessAsync(int userId, int taskId, CancellationToken ct = default)
		=> Table().Include(nameof(TaskList)).AsNoTracking()
			.FirstOrDefaultAsync(t => t.Id == taskId
				&& (t.AssignedToId == userId
					|| t.TaskList.OwnerId == userId
					|| t.TaskList.TaskListShares.Any(s => s.UserId == userId)), ct);

	/// <summary>
	/// کنترل دسترسی کاربر به تسک/
	/// یا مالک لیست باشه یا تسک بهش ارجاع شده یا لیست تسک ها باهاش اشتراک گذاری شده
	/// </summary>
	public Task<bool> HasTaskItemAccessAsync(int userId, int taskId, CancellationToken ct = default)
		=> Table().AnyAsync(t => t.Id == taskId
			&& (t.AssignedToId == userId
				|| t.TaskList.OwnerId == userId
				|| t.TaskList.TaskListShares.Any(s => s.UserId == userId)), ct);
	#endregion
}
