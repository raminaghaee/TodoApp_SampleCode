namespace TodoApp.Business.Admin;

public interface IAdminService
{
	/// <summary>اگر کاربر جاری super_admin نبود استثنای 403 می‌دهد.</summary>
	Task EnsureSuperAdminAsync(int userId, CancellationToken ct = default);
	Task<IReadOnlyList<AdminUserDto>> GetAllUsersAsync(CancellationToken ct = default);
	Task<AdminUserDto> SetActiveAsync(int targetUserId, bool isActive, CancellationToken ct = default);
	Task SetRoleAsync(int targetUserId, string role, CancellationToken ct = default);
}
