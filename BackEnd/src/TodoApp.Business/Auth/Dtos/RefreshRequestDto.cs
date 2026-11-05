namespace TodoApp.Business.Auth;

/// <summary>
/// بدنه اختیاری POST /auth/refresh — مرورگرها از HttpOnly Cookie استفاده می‌کنند؛
/// کلاینت‌های غیرمرورگری می‌توانند توکن را در Body بفرستند.
/// </summary>
public class RefreshRequestDto
{
	public string? RefreshToken { get; set; }
}
