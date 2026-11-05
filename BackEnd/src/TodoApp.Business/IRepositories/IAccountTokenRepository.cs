using TodoApp.Business.Entites;

namespace TodoApp.Business.IRepositories;

/// <summary>
/// مخزن توکن‌های یک‌بارمصرف حساب کاربری.
/// نکته معماری: مثل IRefreshTokenRepository در Business تعریف و در Data پیاده‌سازی می‌شود
/// (Business به Data ارجاع ندارد).
/// </summary>
public interface IAccountTokenRepository
{
	/// <summary>ثبت توکن جدید — فقط TokenHash ذخیره می‌شود.</summary>
	Task AddAsync(AccountToken token, CancellationToken ct = default);

	/// <summary>یافتن توکن با hash و purpose (purpose جزء شرط است تا توکن یک جریان در جریان دیگر مصرف نشود).</summary>
	Task<AccountToken?> GetByTokenHashAsync(string tokenHash, string purpose, CancellationToken ct = default);

	/// <summary>
	/// ابطال همه توکن‌های در انتظارِ همان کاربر/هدف — هنگام ساخت درخواست جدید صدا زده می‌شود
	/// («با ایجاد درخواست جدید، توکن قبلی همان کاربر/هدف باطل شود»).
	/// با ExecuteUpdate اتمیک انجام می‌شود و در تراکنش جاری (در صورت وجود) شرکت می‌کند.
	/// </summary>
	Task InvalidatePendingAsync(int userId, string purpose, CancellationToken ct = default);

	/// <summary>
	/// مصرف اتمیک توکن: فقط اگر مصرف‌نشده و منقضی‌نشده باشد UsedAtUtc را پر می‌کند.
	/// معادل «UPDATE ... WHERE UsedAtUtc IS NULL AND ExpiresAtUtc > @now» —
	/// در برابر مصرف همزمانِ یک توکن امن است و تعداد ردیف‌های واقعاً تغییر‌یافته را برمی‌گرداند.
	/// </summary>
	Task<int> TryConsumeAsync(string tokenHash, string purpose, DateTime nowUtc, CancellationToken ct = default);
}

