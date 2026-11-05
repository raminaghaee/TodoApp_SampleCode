using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TodoApp.Business.Entites;
using TodoApp.Business.IRepositories;
using TodoApp.Business.TaskItems;

namespace TodoApp.Business.Subtasks;

public sealed class SubtaskService : ISubtaskService
{
	#region Fields
	private readonly ITaskItemRepository _TaskItemRepository;
	private readonly ISubtaskRepository _subtaskRepository;
	private readonly ITaskItemService _taskItemService;
	#endregion
	#region Ctors
	public SubtaskService(ITaskItemRepository taskItemRepository, ISubtaskRepository subtaskRepository, ITaskItemService taskItemService)
	{
		_TaskItemRepository = taskItemRepository;
		_subtaskRepository = subtaskRepository;
		_taskItemService = taskItemService;
	}
	#endregion
	#region Get
	/// <summary>
	/// مرتب بر اساس position.
	/// </summary>
	public async Task<IReadOnlyList<SubtaskDto>> GetByTaskAsync(int userId, int taskId, CancellationToken ct = default)
	{
		_ = await GetTaskForUserAsync(userId, taskId, ct)
			?? throw new NotFoundException("کار یافت نشد.");

		var subtasks = await _subtaskRepository.Table()
			.Where(s => s.TaskItemId == taskId)
			.OrderBy(s => s.Position).ThenBy(s => s.Id)
			.ToListAsync(ct);

		return subtasks.Select(ToDto).ToList();
	}
	private async Task<TaskItem?> GetTaskForUserAsync(int userId, int taskId, CancellationToken ct)
		=> await _TaskItemRepository.GetTaskItemWithAccessAsync(userId, taskId, ct);
	private async Task<Subtask?> GetSubtaskForUserAsync(int userId, int subtaskId, CancellationToken ct)
	{
		var subtask = await _subtaskRepository.GetFirstOrDefaultAsync(s => s.Id == subtaskId, ct);
		if (subtask is null)
			return null;

		var task = await _TaskItemRepository.GetTaskItemWithAccessAsync(userId, subtask.TaskItemId, ct);
		if (task is null)
			return null;

		if (!_taskItemService.CanModifyTaskItemAsync(userId, task))
			throw new ForbiddenAccessException("فقط سازنده یا مسئول کار (یا مالک لیست) می‌تواند آن را تغییر دهد.");

		return subtask;
	}
	#endregion
	#region Manipulate
	public async Task<SubtaskDto> CreateAsync(int userId, int taskId, CreateSubtaskRequest request, CancellationToken ct = default)
	{
		var task = await GetTaskForUserAsync(userId, taskId, ct)
			?? throw new NotFoundException("کار یافت نشد.");

		if (!_taskItemService.CanModifyTaskItemAsync(userId, task))
			throw new ForbiddenAccessException("فقط سازنده یا مسئول کار (یا مالک لیست) می‌تواند آن را تغییر دهد.");

		var title = (request.Title ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(title))
			throw new ValidationException("عنوان زیرتسک الزامی است.");
		if (title.Length > 300)
			throw new ValidationException("عنوان زیرتسک حداکثر ۳۰۰ کاراکتر است.");

		var position = request.Position
			?? (await _subtaskRepository.Table().Where(s => s.TaskItemId == taskId).MaxAsync(s => (int?)s.Position, ct) is { } max ? max + 1 : 0);

		var subtask = new Subtask
		{
			TaskItemId = taskId,
			Title = title,
			IsCompleted = false,
			Position = position,
			UserId = userId,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};

		await _subtaskRepository.AddAsync(subtask, ct);
		return ToDto(subtask);
	}

	public async Task<SubtaskDto> UpdateAsync(int userId, int subtaskId, UpdateSubtaskRequest request, CancellationToken ct = default)
	{
		var subtask = await GetSubtaskForUserAsync(userId, subtaskId, ct)
			?? throw new NotFoundException("زیرتسک یافت نشد.");

		var task = await GetTaskForUserAsync(userId, subtask.TaskItemId, ct)
			?? throw new NotFoundException("کار یافت نشد.");

		if (!_taskItemService.CanModifyTaskItemAsync(userId, task))
			throw new ForbiddenAccessException("فقط سازنده یا مسئول کار (یا مالک لیست) می‌تواند آن را تغییر دهد.");

		if (request.Title is not null)
		{
			var title = request.Title.Trim();
			if (string.IsNullOrWhiteSpace(title))
				throw new ValidationException("عنوان زیرتسک نمی‌تواند خالی باشد.");
			if (title.Length > 300)
				throw new ValidationException("عنوان زیرتسک حداکثر ۳۰۰ کاراکتر است.");
			subtask.Title = title;
		}

		if (request.IsCompleted is { } isCompleted)
			subtask.IsCompleted = isCompleted;

		if (request.Position is { } position)
			subtask.Position = position;

		await _subtaskRepository.UpdateAsync(subtask, ct);
		return ToDto(subtask);
	}

	public async Task DeleteAsync(int userId, int subtaskId, CancellationToken ct = default)
	{
		var subtask = await GetSubtaskForUserAsync(userId, subtaskId, ct)
			?? throw new NotFoundException("زیرتسک یافت نشد.");

		await _subtaskRepository.DeleteAsync(subtask, ct);
	} 
	#endregion

	private static SubtaskDto ToDto(Subtask s) => new()
	{
		Id = s.Id,
		TaskItemId = s.TaskItemId,
		Title = s.Title,
		IsCompleted = s.IsCompleted,
		Position = s.Position,
		CreatedBy = s.UserId,
		CreatedAt = s.CreatedAt,
		UpdatedAt = s.UpdatedAt
	};
}

