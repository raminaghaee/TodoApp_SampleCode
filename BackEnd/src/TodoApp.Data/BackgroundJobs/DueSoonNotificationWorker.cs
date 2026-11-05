using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TodoApp.Data.Contexts;

namespace TodoApp.Business.BackgroundJobs;

/// <summary>
/// هر ۱ ساعت کارهای تکمیل‌نشده‌ای که سررسیدشان در بازه امروز تا ۲ روز آینده است -
/// را چک می‌کند و برای مسئول تسك يا ايجاد كننده تسك اعلان می‌سازد. -
/// (assigned_to / created_by)
/// بدون تکرار: اگر برای یک کار قبلاً اعلان due_soon ثبت شده باشد، دوباره ساخته نمی‌شود.
/// </summary>
public sealed class DueSoonNotificationWorker : BackgroundService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<DueSoonNotificationWorker> _logger;

	public DueSoonNotificationWorker(IServiceScopeFactory scopeFactory, ILogger<DueSoonNotificationWorker> logger)
	{
		_scopeFactory = scopeFactory;
		_logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		_logger.LogInformation("DueSoonNotificationWorker started.");

		// اجرای اول در شروع برنامه
		await RunOnceAsync(stoppingToken);

		using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
		while (await timer.WaitForNextTickAsync(stoppingToken))
		{
			try
			{
				await RunOnceAsync(stoppingToken);
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "خطا در ساخت اعلان‌های due_soon.");
			}
		}
	}

	private async Task RunOnceAsync(CancellationToken ct)
	{
		await using var scope = _scopeFactory.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<TodoAppDbContext>();

		var created = await db.Database.ExecuteSqlInterpolatedAsync($@"
			INSERT INTO Notifications (UserId, Type, Title, Body, TaskId, CreatedAt)
			SELECT COALESCE(t.AssignedToId, t.CreatedById),
			       N'due_soon',
			       N'سررسید نزدیک',
			       N'این کار حداکثر تا ۲ روز آینده سررسید می‌شود: ' + t.Title,
			       t.Id,
			       SYSUTCDATETIME()
			FROM Tasks t
			WHERE t.IsCompleted = 0
			  AND t.DueDate IS NOT NULL
			  AND t.DueDate >= CAST(SYSUTCDATETIME() AS date)
			  AND t.DueDate <= DATEADD(day, 2, CAST(SYSUTCDATETIME() AS date))
			  AND NOT EXISTS (
			      SELECT 1 FROM Notifications n
			      WHERE n.TaskId = t.Id AND n.Type = N'due_soon')", ct);

		if (created > 0)
			_logger.LogInformation("{Count} اعلان due_soon ساخته شد.", created);
	}
}

