namespace TodoApp.Business;

/// <summary>
/// 423 Locked — حساب به‌دلیل تلاش‌های ناموفق مکرر موقتاً قفل شده است.
/// پیام عمداً زمان باقی‌مانده‌ی قفل را اعلام نمی‌کند (جلوگیری از Information Leak).
/// </summary>
public class LockedException : Exception
{
	public LockedException() : base("حساب شما موقتاً قفل شده است. لطفاً بعداً دوباره تلاش کنید.") { }
}
