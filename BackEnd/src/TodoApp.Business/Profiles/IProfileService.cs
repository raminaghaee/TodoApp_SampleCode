namespace TodoApp.Business.Profiles;

/// <summary>
/// پروفایل‌ها — هر کاربر خودش + هم‌تیمی‌هایش را می‌بیند؛
/// سوپر ادمين همه را می‌بیند
/// </summary>
public interface IProfileService
{
	/// <summary>
	/// کاربران مرتبط (خودم + هم‌تیمی‌ها - کسانی که لیست / کار مشترک دارن
	/// </summary>
	Task<IReadOnlyList<ProfileDto>> GetRelatedAsync(int userId, CancellationToken ct = default);
	/// <summary>
	/// جستجو با ایمیل دقیق برای اشتراک‌گذاری
	/// </summary>
	Task<IReadOnlyList<ProfileDto>> LookupByEmailAsync(string email, CancellationToken ct = default);
	Task<ProfileDto> UpdateMeAsync(int userId, UpdateProfileRequest request, CancellationToken ct = default);
}
