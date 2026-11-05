using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TodoApp.Business.Entites;
using TodoApp.Business.Users;

namespace TodoApp.Business.Admin;

/// <summary>پنل ادمین — همه عملیات فقط برای super_admin (EnsureSuperAdminAsync در ابتدای هر متد).</summary>
public sealed class AdminService : IAdminService
{
	#region Fields
	private readonly UserManager<AppUser> _userManager;
	private readonly RoleManager<AppRole> _roleManager;
	private readonly IUserDirectory _users;
	#endregion
	#region Ctors
	public AdminService(UserManager<AppUser> userManager, IUserDirectory users, RoleManager<AppRole> roleManager)
	{
		_userManager = userManager;
		_users = users;
		_roleManager = roleManager;
	}
	#endregion
	#region Gets
	/// <summary>اگر کاربر جاری super_admin نبود استثنای 403 می‌دهد.</summary>
	public async Task EnsureSuperAdminAsync(int userId, CancellationToken ct = default)
	{
		var role = await _users.GetRoleAsync(userId, ct);
		if (role != "super_admin")
			throw new ForbiddenAccessException("این عملیات فقط برای سوپرادمین مجاز است.");
	}

	public async Task<IReadOnlyList<AdminUserDto>> GetAllUsersAsync(CancellationToken ct = default)
	{
		return await _userManager.Users
		.OrderByDescending(u => u.CreatedAt)
		.Select(u => new AdminUserDto
		{
			Id = u.Id,
			Email = u.Email,
			DisplayName = u.DisplayName,
			IsActive = u.IsActive,
			CreatedAt = u.CreatedAt,
			Role = u.UserRoles
				.OrderBy(ur => ur.Role.Name == "super_admin" ? 0 : 1)
				.Select(ur => ur.Role.Name)
				.FirstOrDefault() ?? "user"
		})
		.ToListAsync(ct);
	}
	#endregion
	#region Manipulates
	public async Task<AdminUserDto> SetActiveAsync(int targetUserId, bool isActive, CancellationToken ct = default)
	{
		var user = await _userManager.FindByIdAsync(targetUserId.ToString())
			?? throw new NotFoundException("کاربر یافت نشد.");
		user.IsActive = isActive;
		_ = await _userManager.UpdateAsync(user);

		return new AdminUserDto()
		{
			Id = user.Id,
			Email = user.Email,
			DisplayName = user.DisplayName,
			IsActive = user.IsActive,
			CreatedAt = user.CreatedAt,
			Role = user.UserRoles
				.OrderBy(ur => ur.Role.Name == "super_admin" ? 0 : 1)
				.Select(ur => ur.Role.Name)
				.FirstOrDefault() ?? "user"
		};
	}

	public async Task SetRoleAsync(int targetUserId, string role, CancellationToken ct = default)
	{
		var roleName = (role ?? string.Empty).Trim();
		if (roleName is not ("super_admin" or "user"))
			throw new ValidationException("نقش باید یکی از مقادیر super_admin یا user باشد.");

		var user = await _users.GetByIdAsync(targetUserId)
			?? throw new NotFoundException("کاربر یافت نشد.");

		if (await _roleManager.RoleExistsAsync(roleName))
			await _roleManager.CreateAsync(new AppRole() { Name = roleName });

		await _userManager.RemoveFromRoleAsync(user, roleName);

		if (!await _userManager.IsInRoleAsync(user, roleName))
			await _userManager.AddToRoleAsync(user, roleName);
	} 
	#endregion

	internal sealed class AdminUserRow
	{
		public int Id { get; set; }
		public string? Email { get; set; }
		public string? DisplayName { get; set; }
		public bool IsActive { get; set; }
		public DateTime CreatedAt { get; set; }
		public string? Role { get; set; }
	}
}
