namespace TodoApp.Business;

/// <summary>
/// 401 Unauthorized — پیام عمداً کلی است و نمی‌گوید «ایمیل وجود ندارد» یا «رمز اشتباه است»
/// (محافظت در برابر User Enumeration).
/// </summary>
public class InvalidCredentialsException : Exception
{
	public InvalidCredentialsException() : base("ایمیل یا رمز عبور اشتباه است.") { }
}
