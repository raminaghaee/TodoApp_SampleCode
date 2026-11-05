namespace TodoApp.Business;

/// <summary>400 — توکن نامعتبر / منقضی / مصرف‌شده / متعلق به purpose دیگر (پیام عمداً کلی است)</summary>
public class InvalidTokenException : Exception
{
	public InvalidTokenException() : base("لینک نامعتبر است یا منقضی شده است.") { }
}

