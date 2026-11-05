using TodoApp.Business.Entites;

namespace TodoApp.Business.Users;

public interface IUserDirectory
{
	Task<AppUser?> GetByIdAsync(int userId);
	Task<IReadOnlyList<ProfileRow>?> GetByEmailAsync(string email, CancellationToken ct = default);
	/// <summary>
	/// کاربر اگر نقش سوپر ادمین داشته باشد آن را برمیگرداند وگرنه نقش کاربر عادی را برمیگرداند
	/// </summary>
	Task<string> GetRoleAsync(int userId, CancellationToken ct = default);
	Task<bool> ExistsAndActiveAsync(int userId, CancellationToken ct = default);
	/// <summary>
	/// کاربران مرتبط : خودم + هم‌تیمی‌ها -
	/// کسانی که لیست/کار مشترک دارند -
	/// سوپر ادمین همه کاربران را می‌بیند
	/// </summary>
	Task<IReadOnlyList<ProfileRow>> GetRelatedUsersAsync(int userId, CancellationToken ct = default);
}
