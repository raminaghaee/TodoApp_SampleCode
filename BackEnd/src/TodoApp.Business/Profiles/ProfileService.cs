using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TodoApp.Business.Entites;
using TodoApp.Business.Users;

namespace TodoApp.Business.Profiles;

/// <summary>
/// پروفایل‌ها — هر کاربر خودش + هم‌تیمی‌هایش را می‌بیند؛
/// سوپر ادمين همه را می‌بیند
/// </summary>
public sealed class ProfileService : IProfileService
{
	#region Fields
	private readonly UserManager<AppUser> _userManager;
	private readonly IUserDirectory _users;
	#endregion
	#region Ctors
	public ProfileService(UserManager<AppUser> userManager, IUserDirectory users)
	{
		_userManager = userManager;
		_users = users;
	}
	#endregion
	#region Get
	/// <summary>
	/// کاربران مرتبط (خودم + هم‌تیمی‌ها - کسانی که لیست / کار مشترک دارن
	/// </summary>
	public async Task<IReadOnlyList<ProfileDto>> GetRelatedAsync(int userId, CancellationToken ct = default)
	{
		var rows = await _users.GetRelatedUsersAsync(userId, ct);
		return rows.Select(r => new ProfileDto { Id = r.Id, Email = r.Email, DisplayName = r.DisplayName }).ToList();
	}

	/// <summary>
	///  جستجو با ایمیل دقیق برای اشتراک‌گذاری
	/// </summary>
	public async Task<IReadOnlyList<ProfileDto>> LookupByEmailAsync(string email, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(email))
			throw new ValidationException("پارامتر email الزامی است.");

		var rows = await _users.GetByEmailAsync(email.Trim(), ct);
		if (rows is null || rows.Count == 0)
			throw new NotFoundException("کاربری با این ایمیل یافت نشد.");

		return rows
			.Select(p => new ProfileDto()
			{
				Id = p.Id,
				Email = p.Email,
				DisplayName = p.DisplayName,
			})
			.ToList();
	}
	#endregion
	#region Manipulate
	public async Task<ProfileDto> UpdateMeAsync(int userId, UpdateProfileRequest request, CancellationToken ct = default)
	{
		var email = (request.Email ?? string.Empty).Trim();
		if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || email.Length > 256)
			throw new ValidationException("ایمیل معتبر نیست.");

		var displayName = request.DisplayName?.Trim();
		if (displayName is { Length: > 200 })
			throw new ValidationException("نام نمایشی حداکثر ۲۰۰ کاراکتر است.");

		var normalizedEmail = email.ToUpperInvariant();

		if (await _userManager.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail && u.Id != userId, ct))
			throw new ConflictException("این ایمیل قبلاً استفاده شده است.");

		try
		{
			var user = await _userManager.FindByIdAsync(userId.ToString())
				?? throw new NotFoundException("کاربر مورد نظر یافت نشد");
			//TODO: EmailConfirmed دست نمی‌خورد چون طبق تصمیم پروژه تأیید ایمیل نداریم؛
			// اگر روزی تأیید ایمیل فعال شد، اینجا باید 0 شود.
			user.Email = email;
			user.DisplayName = displayName;
			user.NormalizedEmail = normalizedEmail;
			user.NormalizedUserName = normalizedEmail;
			var affected = await _userManager.UpdateAsync(user);

			return new ProfileDto { Id = user.Id, Email = user.Email, DisplayName = user.DisplayName };
		}
		//TODO:
		//catch (SqlException ex) when (ex.Number is 2601 or 2627)
		// ایمیل تکراری هم‌زمان (ایندکس یکتا) — پیشنهاد: ایندکس یکتا روی NormalizedEmail بگذارید
		catch (Exception)
		{
			throw new ConflictException("این ایمیل قبلاً استفاده شده است.");
		}
	}
	#endregion
}
