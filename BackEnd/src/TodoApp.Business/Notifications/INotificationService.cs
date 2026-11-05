namespace TodoApp.Business.Notifications;

public interface INotificationService
{
	/// <summary>
	/// حداکثر ۵۰ مورد، مرتب بر اساس -
	/// مرتب سازي نزولی بر اساس تاريخ ايجاد
	/// .</summary>
	Task<IReadOnlyList<NotificationDto>> GetMineAsync(int userId, CancellationToken ct = default);
	Task<NotificationDto> MarkAsReadAsync(int userId, int notificationId, CancellationToken ct = default);
	Task MarkAllAsReadAsync(int userId, CancellationToken ct = default);
	Task DeleteAsync(int userId, int notificationId, CancellationToken ct = default);
	Task<NotificationDto> CreateAsync(int userId, CreateNotificationRequest request, CancellationToken ct = default);
}
