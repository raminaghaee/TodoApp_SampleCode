using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TodoApp.Business.Entites;
using TodoApp.Business.IRepositories;

namespace TodoApp.Business.Users;

public sealed class UserDirectory : IUserDirectory
{
	#region fields
	private readonly UserManager<AppUser> _userManager;
	private readonly ITaskListShareRepository _taskListShareRepository;
	private readonly ITaskItemRepository _taskItemRepository;
	private readonly ITaskListRepository _taskListRepository;
	#endregion
	#region Ctors
	public UserDirectory(UserManager<AppUser> userManager,
		ITaskListShareRepository taskListShareRepository,
		ITaskItemRepository taskItemRepository,
		ITaskListRepository taskListRepository)
	{
		_userManager = userManager;
		_taskListShareRepository = taskListShareRepository;
		_taskItemRepository = taskItemRepository;
		_taskListRepository = taskListRepository;
	}
	#endregion
	#region Gets
	public async Task<AppUser?> GetByIdAsync(int userId)
	{
		return await _userManager.FindByIdAsync(userId.ToString()) ?? throw new InvalidCredentialsException();
	}

	public async Task<IReadOnlyList<ProfileRow>?> GetByEmailAsync(string email, CancellationToken ct = default)
	{
		return await _userManager.Users
			.Where(u => u.Email!.Trim().ToLower().StartsWith(email.Trim().ToLower()))
			.Select( u =>new ProfileRow
			{
				Id = u.Id,
				Email = u.Email,
				DisplayName = u.DisplayName,
				IsActive = u.IsActive
			})
			.ToListAsync(ct);
	}

	/// <summary>
	/// کاربر اگر نقش سوپر ادمین داشته باشد آن را برمیگرداند وگرنه نقش کاربر عادی را برمیگرداند
	/// </summary>
	public async Task<string> GetRoleAsync(int userId, CancellationToken ct = default)
	{
		var user = await GetByIdAsync(userId);
		var role = await _userManager.GetRolesAsync(user) ?? throw new InvalidCredentialsException();
		return ( role.Count > 1 ? role.FirstOrDefault( r=> r != "super_admin") : role!.FirstOrDefault()) ?? "user";
	}

	public async Task<bool> ExistsAndActiveAsync(int userId, CancellationToken ct = default)
	{
		var user = await _userManager.FindByIdAsync(userId.ToString()) ?? throw new NotFoundException("کاربر یافت نشد.");
		return user.IsActive;
	}

	/// <summary>
	/// کاربران مرتبط : خودم + هم‌تیمی‌ها -
	/// کسانی که لیست/کار مشترک دارند -
	/// سوپر ادمین همه کاربران را می‌بیند
	/// </summary>
	public async Task<IReadOnlyList<ProfileRow>> GetRelatedUsersAsync(int userId, CancellationToken ct = default)
	{
		var role = await GetRoleAsync(userId, ct);
		if (role == "super_admin")
		{
			return await _userManager.Users.AsNoTracking()
				 .OrderBy(u => u.Email)
				 .Select(u => new ProfileRow
				 {
					 Id = u.Id,
					 Email = u.Email,
					 DisplayName = u.DisplayName,
					 IsActive = u.IsActive
				 })
				 .ToListAsync(ct);
		}

		return await _userManager.Users.AsNoTracking()
		.Where( u => 
			u.Id == userId
			// کاربرانی که در لیست‌های من شریک شده‌اند
			|| _taskListShareRepository.Table().Any(ls => ls.UserId == u.Id && ls.TaskList.OwnerId == userId)

			// صاحبان لیست‌هایی که من در آن‌ها شریک شده‌ام
			|| _taskListRepository.Table().Any(l => l.OwnerId == u.Id && l.TaskListShares.Any(ls => ls.UserId == userId))

			// کاربرانی که در تسک‌های من به آن‌ها کار سپرده شده
			|| _taskItemRepository.Table().Any(t => t.AssignedToId == u.Id && t.CreatedById == userId)

			// کاربرانی که به من کار سپرده‌اند
			|| _taskItemRepository.Table().Any(t => t.CreatedById == u.Id && t.AssignedToId == userId)
		)
		.OrderBy(u => u.Email)
		.Select(u => new ProfileRow
		{
			Id = u.Id,
			Email = u.Email,
			DisplayName = u.DisplayName,
			IsActive = u.IsActive
		})
		.ToListAsync(ct);
	}
	#endregion
}
