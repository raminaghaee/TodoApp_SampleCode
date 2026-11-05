namespace TodoApp.Business.Entites;

/// <summary>
/// توکن‌های امنیتی یک‌بارمصرف حساب کاربری (بازیابی رمز / تأیید تغییر ایمیل).
/// نکته امنیتی: هرگز توکن خام ذخیره نمی‌شود — فقط SHA-256 آن (مثل الگوی RefreshToken فعلی پروژه)؛
/// توکن خام فقط در لینک ایمیل وجود دارد و همان لحظه دور انداخته می‌شود.
/// </summary>
public class AccountToken
{
	public int Id { get; set; }

	/// <summary>صاحب توکن</summary>
	public int UserId { get; set; }

	/// <summary>SHA-256 (hex) توکن خام</summary>
	public string TokenHash { get; set; } = default!;

	/// <summary>هدف توکن — مقادیر AccountTokenPurposes؛ مصرف توکنِ purpose دیگر رد می‌شود</summary>
	public string Purpose { get; set; } = default!;

	/// <summary>انقضای مطلق لینک (UTC)</summary>
	public DateTime ExpiresAtUtc { get; set; }

	/// <summary>زمان مصرف (UTC) — null یعنی هنوز مصرف نشده؛ مصرف اتمیک با UPDATE شرطی انجام می‌شود</summary>
	public DateTime? UsedAtUtc { get; set; }

	/// <summary>فقط برای purpose=email_change: ایمیل جدیدِ در انتظار تأیید؛
	/// تا قبل از تأیید، ایمیل قبلی کاربر و ورود با آن فعال می‌ماند.</summary>
	public string? PendingEmail { get; set; }

	// نکته: اگر در پروژه شما BaseEntity/CreatedAt دارید و TodoAppDbContext.BeforeSave آن را پر می‌کند،
	// طبق روال خودتان این موجودیت را هم پوشش دهید — نیازی به تغییر منطق سرویس نیست.
}

/// <summary>مقادیر مجاز Purpose — رشته (نه enum) تا mapping EF ساده بماند.</summary>
public static class AccountTokenPurposes
{
	/// <summary>لینک بازیابی رمز عبور</summary>
	public const string PasswordReset = "password_reset";

	/// <summary>لینک تأیید تغییر ایمیل</summary>
	public const string EmailChange = "email_change";
}
