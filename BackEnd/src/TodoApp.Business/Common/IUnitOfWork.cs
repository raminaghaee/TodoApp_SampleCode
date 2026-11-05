namespace TodoApp.Business.Common;

/// <summary>
/// تراکنش صریح برای عملیاتی که «مصرف توکن + تغییر داده» باید باهم اتمیک باشند
/// (تأیید ایمیل، بازنشانی رمز). پیاده‌سازی در لایه Data روی همان DbContext Scoped فعلی انجام می‌شود؛
/// چون همه مخزن‌ها و UserManager<AppUser> از همان DbContext Scoped استفاده می‌کنند،
/// همه عملیات داخل تراکنش شرکت خواهند کرد.
/// </summary>
public interface IUnitOfWork
{
	Task BeginTransactionAsync(CancellationToken ct = default);
	Task CommitAsync(CancellationToken ct = default);
	Task RollbackAsync(CancellationToken ct = default);
}
