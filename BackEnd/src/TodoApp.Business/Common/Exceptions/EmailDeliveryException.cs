namespace TodoApp.Business;

/// <summary>502 — خطای سرویس ارسال ایمیل؛ پاسخ موفق جعل نمی‌شود و جزئیات فنی فاش نمی‌شود</summary>
public class EmailDeliveryException : Exception
{
	public EmailDeliveryException() : base("ارسال ایمیل با خطا مواجه شد؛ لطفاً بعداً دوباره تلاش کنید.") { }
}

