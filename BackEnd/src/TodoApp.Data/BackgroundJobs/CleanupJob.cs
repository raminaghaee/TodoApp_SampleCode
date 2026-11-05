using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TodoApp.Business.IRepositories;

namespace TodoApp.Data.BackgroundJobs;

/// <summary>
///  (Hard Delete) وظایفی که بیش از ۳۰ روز از حذف منطقی‌شان گذشته
/// روزانه نیمه‌شب اجرا می‌شود (کم‌بارترین ساعت برای کاربران).
///
/// امنیت (یادداشت تسک): حذف فیزیکی غیرقابل بازگشت است — هر دو شرط
/// باید با هم بررسی شوند.
///
/// ExecuteDeleteAsync یک دستور DELETE مستقیم تولید می‌کند (بدون لود Entity در حافظه)
/// و از ChangeTracker عبور می‌کند؛ یعنی BeforeSave (تبدیل Delete به Soft-Delete)
/// روی آن اثری ندارد — دقیقاً همان رفتار مطلوب برای Hard Delete.
///
/// Exception عمداً catch نمی‌شود تا Hangfire سیاست Retry پیش‌فرض را اجرا کند.
/// </summary>
public class CleanupJob
{
	/// <summary>مدت نگهداری وظایف حذف‌شده در Trash (۳۰ روز).</summary>
	public const int RetentionDays = 30;

	private readonly ITaskItemRepository _taskItemRepository;
	private readonly ILogger<CleanupJob> _logger;

	public CleanupJob(ITaskItemRepository taskItemRepository,
		ILogger<CleanupJob> logger)
	{
		_taskItemRepository = taskItemRepository;
		_logger = logger;
	}

	public async Task HardDeleteExpiredTaskItems(CancellationToken ct)
	{
		var threshold = DateTime.UtcNow.AddDays(-RetentionDays);

		var deletedCount = await _taskItemRepository.TableAsNoTracking()
			.IgnoreQueryFilters()
			.Where(t => t.IsDeleted
				&& t.DeletedAt != null
				&& t.DeletedAt < threshold)
			.ExecuteDeleteAsync(ct);

		_logger.LogInformation(
			"CleanupJob: {Count} todo item(s) permanently deleted (IsDeleted=1 AND DeletedAt < {Threshold:yyyy-MM-dd HH:mm} UTC).",
			deletedCount, threshold);
	}

	public static TimeZoneInfo GetRunTimeZone()
	{
		try
		{ return TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time"); }
		catch (TimeZoneNotFoundException) { }

		try
		{ return TimeZoneInfo.FindSystemTimeZoneById("Asia/Tehran"); }
		catch (TimeZoneNotFoundException) { }

		return TimeZoneInfo.Utc;
	}
}
