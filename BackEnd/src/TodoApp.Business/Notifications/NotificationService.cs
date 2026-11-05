using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TodoApp.Business.Entites;
using TodoApp.Business.IRepositories;

namespace TodoApp.Business.Notifications;

public sealed class NotificationService : INotificationService
{
	#region Fields
	private readonly INotificationRepository _notificationRepository;
	private readonly ITaskItemRepository _taskItemRepository;
	#endregion
	#region Ctors
	public NotificationService(INotificationRepository notificationRepository, ITaskItemRepository taskItemRepository)
	{
		_notificationRepository = notificationRepository;
		_taskItemRepository = taskItemRepository;
	}
	#endregion
	#region Get
	/// <summary>
	/// حداکثر ۵۰ مورد، مرتب بر اساس -
	/// مرتب سازي نزولی بر اساس تاريخ ايجاد
	/// .</summary>
	public async Task<IReadOnlyList<NotificationDto>> GetMineAsync(int userId, CancellationToken ct = default)
	{
		var notifications = await _notificationRepository.Table()
			.Where(n => n.UserId == userId && n.ReadAt == null)
			.OrderByDescending(n => n.CreatedAt)
			.Take(50)
			.ToListAsync(ct);

		return notifications.Select(ToDto).ToList();
	} 
	#endregion
	#region Manipulate
	public async Task<NotificationDto> MarkAsReadAsync(int userId, int notificationId, CancellationToken ct = default)
	{
		var notification = await _notificationRepository
			.GetFirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct)
			?? throw new NotFoundException("اعلان یافت نشد.");

		notification.ReadAt ??= DateTime.UtcNow;
		await _notificationRepository.UpdateAsync(notification, ct);
		return ToDto(notification);
	}

	public async Task MarkAllAsReadAsync(int userId, CancellationToken ct = default)
	{
		await _notificationRepository.Table()
			.Where(n => n.UserId == userId && n.ReadAt == null)
			.ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, DateTime.UtcNow), ct);
	}

	public async Task DeleteAsync(int userId, int notificationId, CancellationToken ct = default)
	{
		var notification = await _notificationRepository
			.GetFirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, ct)
			?? throw new NotFoundException("اعلان یافت نشد.");

		await _notificationRepository.DeleteAsync(notification, ct);
	}

	public async Task<NotificationDto> CreateAsync(int userId, CreateNotificationRequest request, CancellationToken ct = default)
	{
		var title = (request.Title ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(title))
			throw new ValidationException("عنوان اعلان الزامی است.");
		if (title.Length > 300)
			throw new ValidationException("عنوان اعلان حداکثر ۳۰۰ کاراکتر است.");

		var type = string.IsNullOrWhiteSpace(request.Type) ? "info" : request.Type.Trim();
		if (type.Length > 50)
			throw new ValidationException("نوع اعلان حداکثر ۵۰ کاراکتر است.");

		if (request.TaskItemId is { } taskId && !await _taskItemRepository.AnyAsync(t => t.Id == taskId, ct))
			throw new ValidationException("کار مرتبط با اعلان یافت نشد.");

		var notification = new Notification
		{
			UserId = userId,
			Type = type,
			Title = title,
			Body = request.Body,
			TaskItemId = request.TaskItemId,
			CreatedAt = DateTime.UtcNow
		};

		await _notificationRepository.AddAsync(notification, ct);
		return ToDto(notification);
	}

	#endregion
	private static NotificationDto ToDto(Notification n) => new()
	{
		Id = n.Id,
		UserId = n.UserId,
		Type = n.Type,
		Title = n.Title,
		Body = n.Body,
		TaskItemId = n.TaskItemId,
		ReadAt = n.ReadAt,
		CreatedAt = n.CreatedAt
	};
}
