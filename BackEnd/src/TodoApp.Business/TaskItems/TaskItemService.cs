using FluentValidation;
using TodoApp.Business.Entites;
using TodoApp.Business.IRepositories;
using TodoApp.Business.Users;

namespace TodoApp.Business.TaskItems;

public sealed class TaskItemService : ITaskItemService
{
	#region Fields
	private readonly IUserDirectory _users;
	private readonly ITaskItemRepository _taskItemRepository;
	private readonly ITaskListRepository _taskListRepository;
	private readonly INotificationRepository _notificationRepository;

	#endregion
	#region Ctors
	public TaskItemService(IUserDirectory users, ITaskItemRepository taskItemRepository, ITaskListRepository taskListRepository, INotificationRepository notificationRepository = null)
	{
		_users = users;
		_taskItemRepository = taskItemRepository;
		_taskListRepository = taskListRepository;
		_notificationRepository = notificationRepository;
	}
	#endregion
	#region Get
	/// <summary>
	/// همه کارهای قابل‌ مشاهده کاربر -
	/// مرتب سازی نزولی بر اساس تاریخ ایجاد
	/// </summary>
	public async Task<IReadOnlyList<TaskItemDto>> GetVisibleAsync(int userId, CancellationToken ct = default)
	{
		var taskItems = await _taskItemRepository.GetVisibleTaskItemAsync(userId, ct);
		return taskItems.Select(ToDto).ToList();
	} 
	#endregion
	#region Manipulate
	public async Task<TaskItemDto> CreateAsync(int userId, CreateTaskItemRequest request, CancellationToken ct = default)
	{
		if (request.TaskListId <= 0)
			throw new ValidationException("شناسه لیست الزامی است.");

		var title = (request.Title ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(title))
			throw new ValidationException("عنوان کار الزامی است.");
		if (title.Length > 500)
			throw new ValidationException("عنوان کار حداکثر ۵۰۰ کاراکتر است.");

		var list = await _taskListRepository.GetFirstOrDefaultAsync(l => l.Id == request.TaskListId, ct)
			?? throw new NotFoundException("لیست یافت نشد.");

		if (!await _taskListRepository.HasTaskListAccessAsync(userId, list.Id, ct))
			throw new NotFoundException("لیست یافت نشد.");

		var assignedTo = NormalizeAssignee(request.AssignedTo);
		if (assignedTo is > 0 && !await _users.ExistsAndActiveAsync(assignedTo.Value, ct))
			throw new ValidationException("کاربر انتخاب‌شده برای ارجاع یافت نشد یا غیرفعال است.");

		var task = new TaskItem
		{
			TaskListId = list.Id,
			CreatedById = userId,
			AssignedToId = assignedTo,
			Title = title,
			IsCompleted = false,
			IsImportant = request.IsImportant ?? false,
			IsMyDay = request.IsMyDay ?? false,
			DueDate = request.DueDate,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};

		await _taskItemRepository.AddAsync(task, ct);

		if (assignedTo is > 0 && assignedTo != userId)
		{
			await _notificationRepository.AddAsync(new Notification
			{
				UserId = assignedTo.Value,
				Type = "task_assigned",
				Title = "کار جدید به شما ارجاع شد",
				Body = task.Title,
				TaskItemId = task.Id,
				CreatedAt = DateTime.UtcNow
			}, ct);
		}
		return ToDto(task);
	}

	/// <summary>
	/// ویرایش بخشی از فیلدها + اعلان هنگام تغییر ارجاع
	/// </summary>
	public async Task<TaskItemDto> UpdateAsync(int userId, int taskId, UpdateTaskItemRequest request, CancellationToken ct = default)
	{
		var task = await _taskItemRepository.GetTaskItemWithAccessAsync(userId, taskId, ct)
			?? throw new NotFoundException("کار یافت نشد.");

		if (!CanModifyTaskItemAsync(userId, task))
			throw new ForbiddenAccessException("فقط سازنده یا مسئول کار (یا مالک لیست) می‌تواند آن را تغییر دهد.");

		if (request.Title is not null)
		{
			var title = request.Title.Trim();
			if (string.IsNullOrWhiteSpace(title))
				throw new ValidationException("عنوان کار نمی‌تواند خالی باشد.");
			if (title.Length > 500)
				throw new ValidationException("عنوان کار حداکثر ۵۰۰ کاراکتر است.");
			task.Title = title;
		}
		//if (task.TaskListId != request.TaskListId)
			//task.TaskListId = request.TaskListId;


		if (request.Notes is not null)
			task.Notes = request.Notes;

		if (request.IsCompleted is { } isCompleted)
		{
			task.IsCompleted = isCompleted;
			task.CompletedAt = isCompleted
				? (request.CompletedAt ?? DateTime.UtcNow)
				: null;
		}

		if (request.IsImportant is { } isImportant)
			task.IsImportant = isImportant;

		if (request.IsMyDay is { } isMyDay)
			task.IsMyDay = isMyDay;

		if (request.ClearDueDate == true)
			task.DueDate = null;
		else if (request.DueDate is { } dueDate)
			task.DueDate = dueDate;

		if (request.AssignedTo is { })
		{
			var target = NormalizeAssignee(request.AssignedTo);
			if (target is > 0 && !await _users.ExistsAndActiveAsync(target.Value, ct))
				throw new ValidationException("کاربر انتخاب‌شده برای ارجاع یافت نشد یا غیرفعال است.");

			if (task.AssignedToId != target)
			{
				task.AssignedToId = target;

				// اعلان فقط وقتی ارجاع تازه و به شخص دیگری است
				if (target is > 0 && target != userId)
				{
					await _notificationRepository.AddAsync(new Notification
					{
						UserId = target.Value,
						Type = "task_assigned",
						Title = "کار جدید به شما ارجاع شد",
						Body = task.Title,
						TaskItemId = task.Id,
						CreatedAt = DateTime.UtcNow
					}, ct);
				}
			}
		}

		task.UpdatedAt = DateTime.UtcNow;
		await _taskItemRepository.UpdateAsync(task, ct);
		return ToDto(task);
	}

	public async Task DeleteAsync(int userId, int taskId, CancellationToken ct = default)
	{
		var task = await _taskItemRepository.GetTaskItemWithAccessAsync(userId, taskId, ct)
			?? throw new NotFoundException("کار یافت نشد.");

		if (!CanModifyTaskItemAsync(userId, task))
			throw new ForbiddenAccessException("فقط سازنده یا مسئول کار (یا مالک لیست) می‌تواند آن را تغییر دهد.");

		await _taskItemRepository.DeleteAsync(task, ct);
	}

	private static int? NormalizeAssignee(int? assignedTo) =>
		assignedTo is > 0 ? assignedTo : null;

	/// <summary>
	/// در صورتی که کاربر سازنده تسک  و یا مالک لیست باشد و یا تسک بهش ارجاع شده باشد قابل ویرایش است
	/// </summary>
	public bool CanModifyTaskItemAsync(int userId, TaskItem task)
		=> task.CreatedById == userId || //سازنده
		   (task.AssignedToId.HasValue && task.AssignedToId == userId ) || //مسئول کار
		   task.TaskList.OwnerId == userId /*مالک لیست کار*/;

	#endregion
	private static TaskItemDto ToDto(TaskItem t) => new()
	{
		Id = t.Id,
		ListId = t.TaskListId,
		CreatedBy = t.CreatedById,
		AssignedTo = t.AssignedToId,
		Title = t.Title,
		Notes = t.Notes,
		IsCompleted = t.IsCompleted,
		IsImportant = t.IsImportant,
		IsMyDay = t.IsMyDay,
		DueDate = t.DueDate,
		CompletedAt = t.CompletedAt,
		CreatedAt = t.CreatedAt,
		UpdatedAt = t.UpdatedAt
	};
}
