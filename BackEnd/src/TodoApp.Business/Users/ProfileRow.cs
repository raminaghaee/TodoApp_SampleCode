namespace TodoApp.Business.Users;
/// <summary>
/// ردیف خوانده‌شده از جدول کاربران
/// — بدون وابستگی به انتیتی کاربر
/// - بدون وابستگی به DBContext
/// </summary>
public sealed class ProfileRow
{
	public int Id { get; set; }
	public string? Email { get; set; }
	public string? DisplayName { get; set; }
	public bool IsActive { get; set; }
}
