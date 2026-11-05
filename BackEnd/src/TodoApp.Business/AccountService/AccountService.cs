using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using TodoApp.Business.Auth;
using TodoApp.Business.Authentication;
using TodoApp.Business.Common;
using TodoApp.Business.Entites;
using TodoApp.Business.IRepositories;

namespace TodoApp.Business.AccountService;


/// <summary>
/// سرویس حساب کاربری — پروفایل، تغییر رمز، تغییر ایمیل و بازیابی رمز.
/// هماهنگ با الگوهای AuthService فعلی:
///   • رمزها با BCrypt (cost=12) هش/بررسی می‌شوند (نه PasswordHasher پیش‌فرض Identity)؛
///   • توکن‌های ایمیل/بازیابی فقط به‌صورت SHA-256 ذخیره می‌شوند؛
///   • ابطال نشست‌ها = ابطال Refresh Token های فعال + افزایش JwtVersion (بطال Access Token ها)؛
///   • رمز خام و توکن خام هرگز لاگ نمی‌شوند.
/// </summary>
public class AccountService
{
	#region Fields
	private const int BcryptWorkFactor = 12; // طبق توصیه OWASP؛ هماهنگ با AuthService
	private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

	private readonly UserManager<AppUser> _userManager;
	private readonly IAccountTokenRepository _accountTokenRepository;
	private readonly IRefreshTokenRepository _refreshTokenRepository;
	private readonly IJwtTokenService _jwtTokenService;
	private readonly IUnitOfWork _unitOfWork;
	private readonly SecuritySettings _security;
	private readonly ILogger<AccountService> _logger;
	#endregion

	#region Ctors
	public AccountService(UserManager<AppUser> userManager,
				  IAccountTokenRepository accountTokenRepository,
				  IRefreshTokenRepository refreshTokenRepository,
				  IJwtTokenService jwtTokenService,
				  IUnitOfWork unitOfWork,
				  IOptions<SecuritySettings> securityOptions,
				  ILogger<AccountService> logger)
	{
		_userManager = userManager;
		_accountTokenRepository = accountTokenRepository;
		_refreshTokenRepository = refreshTokenRepository;
		_jwtTokenService = jwtTokenService;
		_unitOfWork = unitOfWork;
		_security = securityOptions.Value;
		_logger = logger;
	} 
	#endregion

	// ─────────────────────────────────────────────────────────────────────────
	// ۱) پروفایل — فقط نام نمایشی
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// فقط DisplayName به‌روزرسانی می‌شود؛ تغییر مستقیم Email/Role/Id از این مسیر وجود ندارد
	/// (کلاس ورودی فقط display_name می‌پذیرد) و شناسه کاربر از هویت تأییدشده می‌آید.
	/// </summary>
	public async Task<ProfileUserDto> UpdateDisplayNameAsync(int userId, string displayName)
	{
		var user = await _userManager.FindByIdAsync(userId.ToString())
				   ?? throw new InvalidCredentialsException(); // → 401

		user.DisplayName = displayName.Trim();
		var result = await _userManager.UpdateAsync(user);
		if (!result.Succeeded)
		{
			var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
			throw new ValidationException($"به‌روزرسانی نام نمایشی ناموفق بود: {errors}");
		}

		return new ProfileUserDto
		{
			Id = user.Id,
			Email = user.Email ?? string.Empty,
			DisplayName = user.DisplayName ?? string.Empty,
			Role = await ResolveRoleAsync(user)
		};
	}

	// ─────────────────────────────────────────────────────────────────────────
	// ۲) تغییر رمز از پروفایل
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// رمز فعلی با BCrypt بررسی می‌شود؛ در صورت خطا 400 با پیام «رمز فعلی اشتباه است.»
	/// رمز جدید طبق سیاست سرور (Validator) هش و ذخیره می‌شود؛ همه نشست‌های دیگر باطل و
	/// نشست جاری با توکن‌های جدید حفظ می‌شود (کوکی‌ها در Controller ست می‌شوند).
	/// </summary>
	public async Task<AuthResponseDto> ChangePasswordAsync(int userId, ChangePasswordDto dto, CancellationToken ct = default)
	{
		var user = await _userManager.FindByIdAsync(userId.ToString())
				   ?? throw new InvalidCredentialsException();

		if (string.IsNullOrEmpty(user.PasswordHash) ||
			!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
			throw new CurrentPasswordInvalidException(); // → 400 ProblemDetails

		user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: BcryptWorkFactor);
		user.FailedLoginAttempts = 0;
		user.LockoutEnd = null;

		// ابطال همه نشست‌ها (شامل UpdateAsync کاربر و JwtVersion++)
		await RevokeAllSessionsAsync(user, ct);

		// حفظ نشست جاری: توکن‌های جدید برای همین کاربر صادر می‌شود
		return await IssueNewSessionAsync(user, ct);
	}

	// ─────────────────────────────────────────────────────────────────────────
	// ۳) درخواست تغییر ایمیل
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// رمز فعلی بررسی؛ ایمیل normalize و یکتایی آن کنترل می‌شود (کنترل نهایی هنگام تأیید + unique index)؛
	/// ایمیل جدید به‌صورت Pending روی توکن یک‌بارمصرف نگه داشته می‌شود و تا تأیید، ایمیل قبلی و
	/// ورود با آن فعال می‌ماند. لینک به ایمیل «جدید» و هشدار به ایمیل «قبلی» ارسال می‌شود.
	/// </summary>
	public async Task<MessageResponseDto> ChangeEmailAsync(int userId, ChangeEmailDto dto, CancellationToken ct = default)
	{
		var user = await _userManager.FindByIdAsync(userId.ToString())
				   ?? throw new InvalidCredentialsException();

		// ۱) رمز فعلی
		if (string.IsNullOrEmpty(user.PasswordHash) ||
			!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
			throw new CurrentPasswordInvalidException();

		// ۲) normalize — یکسان با رفتار NormalizedEmail در ثبت‌نام فعلی
		var newEmail = dto.NewEmail.Trim().ToLowerInvariant();

		if (string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase))
			throw new ValidationException("ایمیل جدید نباید با ایمیل فعلی یکسان باشد.");

		// ۳) یکتایی (چک اولیه؛ چک نهایی هنگام تأیید انجام می‌شود)
		if (await IsEmailTakenAsync(newEmail, user.Id, ct))
			throw new ConflictException("کاربری با این ایمیل قبلاً ثبت‌نام کرده است."); // → 409

		// ۴) redirect_url آزاد پذیرفته نمی‌شود — فقط origin/مسیرهای فهرست مجاز
		var confirmLinkBase = ResolveAllowedLinkBase(dto.RedirectUrl, _security.ConfirmEmailPath);

		// ۵) با هر درخواست جدید، توکن‌های قبلی همان کاربر/هدف ابطال می‌شوند
		await _accountTokenRepository.InvalidatePendingAsync(user.Id, AccountTokenPurposes.EmailChange, ct);

		var rawToken = GenerateUrlSafeToken();
		await _accountTokenRepository.AddAsync(new AccountToken
		{
			UserId = user.Id,
			TokenHash = Sha256Hex(rawToken), // فقط hash — توکن خام ذخیره/لاگ نمی‌شود
			Purpose = AccountTokenPurposes.EmailChange,
			ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_security.EmailChangeTokenLifetimeMinutes),
			PendingEmail = newEmail
		}, ct);

		//TODO:Version 2.0
		// ۶) لینک به ایمیل جدید — ارسال واقعی باید پذیرفته شود وگرنه پاسخ موفق جعل نشود
		//var confirmLink = $"{confirmLinkBase}#token={Uri.EscapeDataString(rawToken)}";
		//await SendAccountEmailAsync(newEmail, "تأیید تغییر ایمیل",
		//	AccountEmailTemplates.EmailChangeConfirmation(user.DisplayName ?? string.Empty, confirmLink,
		//		FormatLifetime(_security.EmailChangeTokenLifetimeMinutes)),
		//	user.Id, ct);

		//// ۷) هشدار به ایمیل قبلی — حیاتی نیست؛ خطایش جریان اصلی را نمی‌شکند
		//try
		//{
		//	await _emailSender.SendAsync(user.Email!, "درخواست تغییر ایمیل حساب کاربری",
		//		AccountEmailTemplates.EmailChangeNoticeToOldAddress(user.Email!, newEmail), ct);
		//}
		//catch (Exception ex)
		//{
		//	_logger.LogWarning(ex, "ارسال هشدار تغییر ایمیل به آدرس قبلی ناموفق بود. UserId={UserId}", user.Id);
		//}
		//موقت و بعد از اتصال ایمیل پاک شود
		await ConfirmEmailChangeAsync(new ConfirmEmailDto() { Token = rawToken}, ct);

		return new MessageResponseDto { Message = "ایمیل تغییر کرد صفحه را بروز کنید" };
		//return new MessageResponseDto { Message = "لینک تأیید ارسال شد." };
	}

	// ─────────────────────────────────────────────────────────────────────────
	// ۴) تأیید ایمیل جدید
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// مصرف اتمیک توکن (در تراکنش) → بررسی مجدد یکتایی → به‌روزرسانی یکجای Email و UserName
	/// (چون در این پروژه UserName = Email) → ابطال همه نشست‌ها تا کاربر با ایمیل جدید وارد شود.
	/// اگر هر مرحله‌ای شکست بخورد، مصرف توکن هم برمی‌گردد (Rollback).
	/// </summary>
	public async Task ConfirmEmailChangeAsync(ConfirmEmailDto dto, CancellationToken ct = default)
	{
		var tokenHash = Sha256Hex(dto.Token);

		await _unitOfWork.BeginTransactionAsync(ct);
		try
		{
			// ۱) مصرف اتمیک: فقط اگر مصرف‌نشده و منقضی‌نشده باشد (امن در برابر درخواست‌های همزمان)؛
			//    purpose جزء شرط است تا توکن جریان دیگر (مثلاً بازیابی رمز) این‌جا پذیرفته نشود.
			var affected = await _accountTokenRepository.TryConsumeAsync(
				tokenHash, AccountTokenPurposes.EmailChange, DateTime.UtcNow, ct);
			if (affected == 0)
				throw new InvalidTokenException(); // 400 پیام کلی داده شود

			var token = await _accountTokenRepository.GetByTokenHashAsync(tokenHash, AccountTokenPurposes.EmailChange, ct)
						?? throw new InvalidTokenException();

			var user = await _userManager.FindByIdAsync(token.UserId.ToString());
			if (user is null || string.IsNullOrEmpty(token.PendingEmail))
				throw new InvalidTokenException();

			var newEmail = token.PendingEmail;

			// ۲) یکتایی دوباره — در برابر ثبت‌نام همزمان با همان ایمیل
			if (await IsEmailTakenAsync(newEmail, user.Id, ct))
				throw new ConflictException("کاربری با این ایمیل قبلاً ثبت‌نام کرده است."); // → 409

			// ۳) به‌روزرسانی هویت: ایمیل + UserName (برابر ایمیل) یکجا و همگام
			var setEmail = await _userManager.SetEmailAsync(user, newEmail);
			if (!setEmail.Succeeded)
				throw new ValidationException(string.Join(" | ", setEmail.Errors.Select(e => e.Description)));

			var setUserName = await _userManager.SetUserNameAsync(user, newEmail);
			if (!setUserName.Succeeded)
				throw new ValidationException(string.Join(" | ", setUserName.Errors.Select(e => e.Description)));

			// SetEmailAsync به‌صورت پیش‌فرض EmailConfirmed را false می‌کند؛ آدرس جدید همین‌جا تأیید شده است.
			user.EmailConfirmed = true;
			// محافظت اضافی: Normalized ها حتماً با ایمیل جدید همگام بمانند
			user.NormalizedEmail = newEmail.ToUpperInvariant();
			user.NormalizedUserName = newEmail.ToUpperInvariant();

			// ۴) ابطال همه نشست‌ها — کوکی‌ها در Controller پاک می‌شوند تا با ایمیل جدید وارد شود
			var activeTokens = await _refreshTokenRepository.GetActiveByUserIdAsync(user.Id, ct);
			var now = DateTime.UtcNow;
			foreach (var t in activeTokens)
			{
				t.IsRevoked = true;
				t.RevokedAtUtc = now;
			}
			await _refreshTokenRepository.UpdateRangeAsync(activeTokens, ct);

			user.JwtVersion++;
			await _userManager.UpdateAsync(user);

			// سایر توکن‌های email_change در انتظار هم باطل شوند
			await _accountTokenRepository.InvalidatePendingAsync(user.Id, AccountTokenPurposes.EmailChange, ct);

			await _unitOfWork.CommitAsync(ct);
		}
		catch
		{
			// مصرف توکن و تغییر داده باید اتمیک باشند؛ هر خطا → برگرداندن کل تراکنش
			await _unitOfWork.RollbackAsync(ct);
			throw;
		}
	}

	// ─────────────────────────────────────────────────────────────────────────
	// ۵) فراموشی رمز
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// پاسخ همیشه عمومی و یکسان است (افشای وجود حساب ممنوع). لینک فقط به ایمیل واقعیِ ثبت‌شده
	/// و فعال ساخته و ارسال می‌شود؛ خطای سرویس ارسال → 502 و پاسخ موفق جعل نمی‌شود.
	/// </summary>
	public async Task<MessageResponseDto> ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct = default)
	{
		const string genericMessage = "اگر حساب موجود باشد، لینک ارسال می‌شود.";

		// ۱) اعتبارسنجی redirect_url قبل از هر چیز — نتیجه مستقل از وجود/عدم وجود حساب است
		var resetLinkBase = ResolveAllowedLinkBase(dto.RedirectUrl, _security.PasswordResetPath);

		var email = dto.Email.Trim().ToLowerInvariant();
		var user = await _userManager.FindByEmailAsync(email);

		// «ثبت‌شده و تأییدشده» — چون ثبت‌نام فعلی شما تأیید ایمیل ندارد، پیش‌فرض فقط «موجود و فعال» است؛
		// پس از فعال‌سازی تأیید ایمیل در ثبت‌نام، RequireConfirmedEmailForReset را true کنید.
		var sendAllowed = user is not null
						  && user.IsActive
						  && (!_security.RequireConfirmedEmailForReset || user.EmailConfirmed);

		if (sendAllowed)
		{
			// ۲) توکن قبلی همان کاربر/هدف ابطال و توکن جدید (فقط hash) ثبت می‌شود
			await _accountTokenRepository.InvalidatePendingAsync(user!.Id, AccountTokenPurposes.PasswordReset, ct);

			var rawToken = GenerateUrlSafeToken();
			await _accountTokenRepository.AddAsync(new AccountToken
			{
				UserId = user.Id,
				TokenHash = Sha256Hex(rawToken),
				Purpose = AccountTokenPurposes.PasswordReset,
				ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_security.PasswordResetTokenLifetimeMinutes)
			}, ct);

			// ۳) ارسال واقعی — خطا جنس پاسخ را عوض می‌کند (جعل موفقیت ممنوع)
			var resetLink = $"{resetLinkBase}#type=recovery&token={Uri.EscapeDataString(rawToken)}";
			await SendAccountEmailAsync(user.Email!, "بازیابی رمز عبور",
				AccountEmailTemplates.PasswordReset(user.DisplayName ?? string.Empty, resetLink,
					FormatLifetime(_security.PasswordResetTokenLifetimeMinutes)),
				user.Id, ct);
		}
		else
		{
			// هم‌زمان‌سازی تقریبی زمان پاسخ برای حساب ناموجود/غیرفعال؛
			// محافظت اصلی در برابر Enumeration همان Rate Limit مسیر است.
			await Task.Delay(Random.Shared.Next(300, 800), ct);
		}

		return new MessageResponseDto { Message = genericMessage };
	}

	// ─────────────────────────────────────────────────────────────────────────
	// ۶) بازنشانی رمز با توکن بازیابی
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// مصرف اتمیک توکن در تراکنش (رمز فعلی گرفته نمی‌شود) → هش رمز جدید → ابطال همه نشست‌ها و
	/// refresh token ها و بطال access token ها با JwtVersion++ → بدون ورود خودکار.
	/// </summary>
	public async Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default)
	{
		var tokenHash = Sha256Hex(dto.Token);

		await _unitOfWork.BeginTransactionAsync(ct);
		try
		{
			var affected = await _accountTokenRepository.TryConsumeAsync(
				tokenHash, AccountTokenPurposes.PasswordReset, DateTime.UtcNow, ct);
			if (affected == 0)
				throw new InvalidTokenException();

			var token = await _accountTokenRepository.GetByTokenHashAsync(tokenHash, AccountTokenPurposes.PasswordReset, ct)
						?? throw new InvalidTokenException();

			var user = await _userManager.FindByIdAsync(token.UserId.ToString());
			if (user is null)
				throw new InvalidTokenException();

			// هش با همان الگوریتم پروژه؛ رمز فعلی در این جریان درخواست نمی‌شود
			user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword, workFactor: BcryptWorkFactor);
			user.FailedLoginAttempts = 0;
			user.LockoutEnd = null;

			var activeTokens = await _refreshTokenRepository.GetActiveByUserIdAsync(user.Id, ct);
			var now = DateTime.UtcNow;
			foreach (var t in activeTokens)
			{
				t.IsRevoked = true;
				t.RevokedAtUtc = now;
			}
			await _refreshTokenRepository.UpdateRangeAsync(activeTokens, ct);

			user.JwtVersion++; // همه Access Token های موجود باطل می‌شوند
			await _userManager.UpdateAsync(user);

			// سایر لینک‌های بازیابیِ باز هم باطل شوند
			await _accountTokenRepository.InvalidatePendingAsync(user.Id, AccountTokenPurposes.PasswordReset, ct);

			await _unitOfWork.CommitAsync(ct);
		}
		catch
		{
			await _unitOfWork.RollbackAsync(ct);
			throw;
		}
	}


	/// <summary>ابطال همه نشست‌ها: refresh token های فعال + افزایش JwtVersion (بطال access token ها).</summary>
	private async Task RevokeAllSessionsAsync(AppUser user, CancellationToken ct)
	{
		var activeTokens = await _refreshTokenRepository.GetActiveByUserIdAsync(user.Id, ct);
		var now = DateTime.UtcNow;
		foreach (var token in activeTokens)
		{
			token.IsRevoked = true;
			token.RevokedAtUtc = now;
		}
		await _refreshTokenRepository.UpdateRangeAsync(activeTokens, ct);

		user.JwtVersion++;
		await _userManager.UpdateAsync(user);
	}

	/// <summary>صدور نشست جدید برای کاربر (الگوی Register/Login فعلی — فقط hash ذخیره می‌شود).</summary>
	private async Task<AuthResponseDto> IssueNewSessionAsync(AppUser user, CancellationToken ct)
	{
		var (accessToken, accessTokenExpiresAtUtc) = _jwtTokenService.GenerateAccessToken(user);
		var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
		var refreshTokenExpiresAtUtc = DateTime.UtcNow.Add(RefreshTokenLifetime);

		await _refreshTokenRepository.AddAsync(new RefreshToken
		{
			UserId = user.Id,
			TokenHash = Sha256Hex(refreshToken),
			ExpiresAtUtc = refreshTokenExpiresAtUtc
		}, ct);

		return new AuthResponseDto
		{
			User = new UserDto { Id = user.Id, Email = user.Email! },
			AccessToken = accessToken,
			RefreshToken = refreshToken,
			AccessTokenExpiresAt = accessTokenExpiresAtUtc,
			RefreshTokenExpiresAt = refreshTokenExpiresAtUtc
		};
	}

	private async Task<bool> IsEmailTakenAsync(string normalizedLowerEmail, int exceptUserId, CancellationToken ct)
	{
		var normalized = normalizedLowerEmail.ToUpperInvariant(); // یکسان با UpperInvariantLookupNormalizer
		return await _userManager.Users.AnyAsync(u =>
			u.Id != exceptUserId &&
			(u.NormalizedEmail == normalized ||
			 (u.Email != null && u.Email.Trim().ToLower() == normalizedLowerEmail)), ct);
	}

	/// <summary>مبنای مجاز لینک: origin باید در فهرست مجاز و مسیر با مسیر مجازِ همان جریان یکی باشد.</summary>
	private string ResolveAllowedLinkBase(string? redirectUrl, string allowedPath)
	{
		if (RedirectUrlValidator.TryGetAllowedBase(redirectUrl, _security.AllowedRedirectOrigins, allowedPath, out var origin))
			return $"{origin}{allowedPath}";

		throw new ValidationException("آدرس بازگشت (redirect_url) مجاز نیست.");
	}

	/// <summary>ارسال ایمیل حساس؛ خطا به EmailDeliveryException نگاشت می‌شود (بدون افشای جزئیات فنی).</summary>
	private async Task SendAccountEmailAsync(string toEmail, string subject, string htmlBody, int userId, CancellationToken ct)
	{
		try
		{
			//TODO : Version v2.0
			//پس از فعال سازی ارسال ایمیل فعال گردد
			//await _emailSender.SendAsync(toEmail, subject, htmlBody, ct);
			throw new NotImplementedException("این امکان در ورژن بعدی در دسترسی قرار خواهد گرفت");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "ارسال ایمیل «{Subject}» برای کاربر {UserId} ناموفق بود.", subject, userId);
			throw new EmailDeliveryException(); // → 502
		}
	}

	/// <summary>توکن تصادفی رمزنگاری‌شده ۲۵۶ بیتی (Base64Url برای قرارگیری امن در URL/fragment).</summary>
	private static string GenerateUrlSafeToken()
		=> Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
				  .TrimEnd('=').Replace('+', '-').Replace('/', '_');

	private static string Sha256Hex(string value)
		=> Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

	private static string FormatLifetime(int minutes)
		=> minutes >= 1440 && minutes % 1440 == 0 ? $"{minutes / 1440} روز"
		 : minutes >= 60 && minutes % 60 == 0 ? $"{minutes / 60} ساعت"
		 : $"{minutes} دقیقه";

	/// <summary>همان قاعده UserDirectory.GetRoleAsync: اگر چند نقش داشت، نقش غیر super_admin برگردانده می‌شود.</summary>
	private async Task<string> ResolveRoleAsync(AppUser user)
	{
		var roles = await _userManager.GetRolesAsync(user);
		return (roles.Count > 1 ? roles.FirstOrDefault(r => r != "super_admin") : roles.FirstOrDefault()) ?? "user";
	}
}
