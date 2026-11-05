namespace TodoApp.Business.Auth;


/// <summary>
/// تنظیمات امنیتی جریان‌های حساب کاربری — از appsettings بخش «Security» خوانده می‌شود.
/// </summary>
public class SecuritySettings
{
	public const string SectionName = "Security";

	/// <summary>
	/// مبنای جایگزین برای ساخت لینک اگر کلاینت redirect_url نفرستد (اختیاری)؛
	/// در جریان فعلی redirect_url از بدنه درخواست می‌آید و حتماً باید با فهرست مجاز تطبیق داده شود.
	/// </summary>
	public string FrontendBaseUrl { get; set; } = string.Empty;

	/// <summary>
	/// فهرست مجاز origin های فرانت (scheme + host + port) — مثال:
	/// [ "https://app.example.com", "http://localhost:5173" ]
	/// redirect_url آزاد پذیرفته نمی‌شود؛ فقط این origin ها (با مسیر مجاز هر جریان) قبول‌اند.
	/// </summary>
	public string[] AllowedRedirectOrigins { get; set; } = [];

	/// <summary>مسیر مجاز صفحه بازنشانی رمز در فرانت</summary>
	public string PasswordResetPath { get; set; } = "/reset-password";

	/// <summary>مسیر مجاز صفحه تأیید ایمیل در فرانت</summary>
	public string ConfirmEmailPath { get; set; } = "/confirm-email";

	/// <summary>اعتبار لینک بازیابی رمز (دقیقه) — پیش‌فرض ۶۰ دقیقه</summary>
	public int PasswordResetTokenLifetimeMinutes { get; set; } = 60;

	/// <summary>اعتبار لینک تأیید تغییر ایمیل (دقیقه) — پیش‌فرض ۲۴ ساعت</summary>
	public int EmailChangeTokenLifetimeMinutes { get; set; } = 60 * 24;

	/// <summary>
	/// اگر true باشد، لینک بازیابی فقط به حساب‌هایی ارسال می‌شود که EmailConfirmed دارند.
	/// چون ثبت‌نام فعلی پروژه شما تأیید ایمیل ندارد، پیش‌فرض false است؛
	/// بعد از فعال‌سازی تأیید ایمیل در ثبت‌نام می‌توانید روشن کنید.
	/// </summary>
	public bool RequireConfirmedEmailForReset { get; set; } = false;

	/// <summary>حداقل طول رمز — با قواعد RegisterDtoValidator فعلی پروژه هماهنگ نگه داشته شود</summary>
	public int PasswordMinLength { get; set; } = 8;
}

