using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TodoApp.Business.Entites;
using TodoApp.Business.IRepositories;
using TodoApp.Business.Users;

namespace TodoApp.Business.TaskLists;

public sealed class TaskListService : ITaskListService
{
	#region fields
	private readonly IUserDirectory _users;
	private readonly ITaskListShareRepository _taskListShareRepository;
	private readonly ITaskListRepository _taskListRepository;
	#endregion
	#region Ctors
	public TaskListService(ITaskListShareRepository taskListShareRepository, ITaskListRepository taskListRepository, IUserDirectory users)
	{
		_taskListShareRepository = taskListShareRepository;
		_taskListRepository = taskListRepository;
		_users = users;
	}
	#endregion
	#region Gets
	/// <summary>
	/// لیست‌های من = مالک + اشتراک‌شده -
	/// مرتب سازی به صورت صعودی بر اساس تاریخ ایجاد
	/// </summary>
	public async Task<IReadOnlyList<TaskListDto?>> GetMyTaskListsAsync(int userId, CancellationToken ct = default)
	{
		//TODO : صفحه بندی واقعی در سمت بک انجام شود الان فقط سمت فرانت انجام میشه و کل دیتا میره سمت فرانت
		var sharedListIds = _taskListShareRepository.Table()
			.Where(s => s.UserId == userId)
			.Select(s => s.TaskListId);

		var lists = await _taskListRepository.Table()
			.Where(l => l.OwnerId == userId || sharedListIds.Contains(l.Id))
			.OrderBy(l => l.CreatedAt)
			.ToListAsync(ct);

		return lists.Select(ToDto).ToList();
	}
	/// <summary>
	/// همه اشتراک‌ های ، تسک لیست ، قابل‌مشاهده برای من
	/// </summary>
	public async Task<IReadOnlyList<TaskListShareDto>> GetVisibleTaskListSharesAsync(int userId, CancellationToken ct = default)
	{
		var myOwnedIds = _taskListRepository.Table().Where(l => l.OwnerId == userId).Select(l => l.Id);

		var shares = _taskListShareRepository.Table()
			.Where(s => s.UserId == userId || myOwnedIds.Contains(s.TaskListId))
			.OrderBy(s => s.CreatedAt)
			.ToList();

		return shares.Select(s => new TaskListShareDto
		{
			Id = s.Id,
			ListId = s.TaskListId,
			UserId = s.UserId,
			CreatedAt = s.CreatedAt
		}).ToList();
	}
	#endregion
	#region Manipulate
	public async Task<TaskListDto> CreateAsync(int userId, CreateTaskListRequest request, CancellationToken ct = default)
	{
		var name = (request.Name ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(name))
			throw new ValidationException("نام لیست الزامی است.");
		if (name.Length > 200)
			throw new ValidationException("نام لیست حداکثر ۲۰۰ کاراکتر است.");

		var color = ValidateColor(request.Color);

		var list = new TaskList
		{
			OwnerId = userId,
			Name = name,
			Color = color,
			CreatedAt = DateTime.UtcNow
		};

		await _taskListRepository.AddAsync(list, ct);
		return ToDto(list);
	}

	/// <summary>
	/// ویرایش فقط توسط مالک +کاربر مشترک
	/// </summary>
	public async Task<TaskListDto> UpdateAsync(int userId, int listId, UpdateTaskListRequest request, CancellationToken ct = default)
	{
		var taskList = await _taskListRepository.GetFirstOrDefaultAsync(l => l.Id == listId, ct)
			?? throw new NotFoundException("لیست یافت نشد.");

		if (!await _taskListRepository.HasTaskListAccessAsync(userId, listId, ct))
			throw new NotFoundException("لیست یافت نشد.");

		if (request.Name is not null)
		{
			var name = request.Name.Trim();
			if (string.IsNullOrWhiteSpace(name))
				throw new ValidationException("نام لیست نمی‌تواند خالی باشد.");
			if (name.Length > 200)
				throw new ValidationException("نام لیست حداکثر ۲۰۰ کاراکتر است.");
			taskList.Name = name;
		}

		if (request.Color is not null)
			taskList.Color = ValidateColor(request.Color);

		await _taskListRepository.UpdateAsync(taskList, ct);
		return ToDto(taskList);
	}

	/// <summary>
	/// حذف فقط توسط مالک.
	/// </summary>
	public async Task DeleteAsync(int userId, int listId, CancellationToken ct = default)
	{
		var taskList = await _taskListRepository.GetFirstOrDefaultAsync(l => l.Id == listId, ct)
			?? throw new NotFoundException("لیست یافت نشد.");

		if (taskList.OwnerId != userId)
			throw new ForbiddenAccessException("حذف لیست فقط توسط مالک امکان‌پذیر است.");

		await _taskListRepository.DeleteAsync(taskList, ct);
	}

	/// <summary>
	/// اشتراک‌گذاری لیست فقط توسط مالک
	/// </summary>
	public async Task<TaskListShareDto> CreateShareAsync(int userId, int listId, CreateTaskListShareRequest request, CancellationToken ct = default)
	{
		var list = await _taskListRepository.GetFirstOrDefaultAsync(l => l.Id == listId, ct)
			?? throw new NotFoundException("لیست یافت نشد.");

		if (list.OwnerId != userId)
			throw new ForbiddenAccessException("اشتراک‌گذاری لیست فقط توسط مالک امکان‌پذیر است.");

		if (request.UserId <= 0)
			throw new ValidationException("شناسه کاربر نامعتبر است.");

		if (!await _users.ExistsAndActiveAsync(request.UserId, ct))
			throw new ValidationException("کاربر موردنظر یافت نشد یا غیرفعال است.");

		var exists = await _taskListShareRepository
			.AnyAsync(s => s.TaskListId == listId && s.UserId == request.UserId, ct);
		if (exists)
			throw new ConflictException("این لیست قبلاً با این کاربر به اشتراک گذاشته شده است.");

		var share = new TaskListShare
		{
			TaskListId = listId,
			UserId = request.UserId,
			CreatedAt = DateTime.UtcNow
		};

		await _taskListShareRepository.AddAsync(share, ct);

		return new TaskListShareDto
		{
			Id = share.Id,
			ListId = share.TaskListId,
			UserId = share.UserId,
			CreatedAt = share.CreatedAt
		};
	}

	/// <summary>
	/// حذف اشتراک فقط مالک
	/// </summary>
	public async Task DeleteShareAsync(int userId, int listId, int targetUserId, CancellationToken ct = default)
	{
		var list = await _taskListRepository.GetFirstOrDefaultAsync(l => l.Id == listId, ct)
			?? throw new NotFoundException("لیست یافت نشد.");

		if (list.OwnerId != userId)
			throw new ForbiddenAccessException("حذف اشتراک فقط توسط مالک لیست امکان‌پذیر است.");

		var share = await _taskListShareRepository
			.GetFirstOrDefaultAsync(s => s.TaskListId == listId && s.UserId == targetUserId, ct)
			?? throw new NotFoundException("اشتراک یافت نشد.");

		await _taskListShareRepository.DeleteAsync(share, ct);
	}

	private static string ValidateColor(string? color)
	{
		var allowed = new[] { "blue", "teal", "violet", "amber", "rose" };
		var value = (color ?? "blue").Trim().ToLowerInvariant();
		return !allowed.Contains(value)
			? throw new ValidationException("رنگ باید یکی از مقادیر blue, teal, violet, amber, rose باشد.")
			: value;
	} 
	#endregion

	private static TaskListDto ToDto(TaskList list) => new()
	{
		Id = list.Id,
		OwnerId = list.OwnerId,
		Name = list.Name,
		Color = list.Color,
		CreatedAt = list.CreatedAt
	};
}
