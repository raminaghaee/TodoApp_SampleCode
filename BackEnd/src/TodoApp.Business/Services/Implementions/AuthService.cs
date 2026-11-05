using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TodoApp.Business.Auth;
using TodoApp.Business.Authentication;
using TodoApp.Business.Entites;
using TodoApp.Business.IRepositories;

namespace TodoApp.Business.Services;

/// <summary>
/// نکته معماری: Business به Data ارجاع ندارد (Data → Business است)،
/// بنابراین ذخیره‌سازی Refresh Token از طریق IRefreshTokenRepository انجام می‌شود، نه DbContext.
/// </summary>
public partial class AuthService
{
	#region Fields
	private const int BcryptWorkFactor = 12; // طبق توصیه OWASP؛ هر هش ~۲۵۰ms
	private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);
	private const int MaxFailedLoginAttempts = 5;
	private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
	private readonly UserManager<AppUser> _userManager;
	private readonly IJwtTokenService _jwtTokenService;
	private readonly IRefreshTokenRepository _refreshTokenRepository; 
	#endregion
	#region Ctors
	public AuthService(UserManager<AppUser> userManager,
			   IJwtTokenService jwtTokenService,
			   IRefreshTokenRepository refreshTokenRepository)
	{
		_userManager = userManager;
		_jwtTokenService = jwtTokenService;
		_refreshTokenRepository = refreshTokenRepository;
	} 
	#endregion

	public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken cancellationToken = default)
	{
		// ۱) بررسی یکتایی ایمیل:
		//    FindByEmailAsync با NormalizedEmail (ToUpperInvariant) مقایسه می‌کند؛
		//    پس Test@example.com و test@example.com یکسان تلقی می‌شوند.
		var existingUser = await _userManager.FindByEmailAsync(dto.Email);
		if (existingUser is not null)
		{
			// محافظت در برابر Timing Attack:
			// یک هش بی‌اثر انجام می‌دهیم تا زمان پاسخ 409 هم‌زمان با مسیر موفق (~۲۵۰ms) باشد.
			BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: BcryptWorkFactor);

			throw new ConflictException("کاربری با این ایمیل قبلاً ثبت‌نام کرده است."); // → 409
		}

		// ۲) ایجاد کاربر (UserName = Email)
		var user = new AppUser
		{
			UserName = dto.Email,
			Email = dto.Email,
			IsActive = true,
			// ۳) هش رمز عبور با BCrypt (cost=12) — نه PasswordHasher پیش‌فرض Identity (PBKDF2).
			//    چون CreateAsync بدون پارامتر password صدا زده می‌شود، هش ما بازنویسی نمی‌شود.
			PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: BcryptWorkFactor),
		};

		var isFirstUser = !await _userManager.Users.AnyAsync(cancellationToken);
		var roleToAssign = isFirstUser ? "super_admin" : "user";

		// ۴) ایجاد کاربر از طریق UserManager
		//    (این متد خودش SaveChanges انجام می‌دهد و NormalizedEmail/NormalizedUserName را پر می‌کند)
		var createResult = await _userManager.CreateAsync(user);
		if (!createResult.Succeeded)
		{
			var errors = string.Join(" | ", createResult.Errors.Select(e => e.Description));
			throw new ValidationException(errors); // → 400
		}


		var roleResult = await _userManager.AddToRoleAsync(user, roleToAssign);
		if (!roleResult.Succeeded)
		{
			// اگر اضافه کردن نقش خطا داد (مثلاً نقش در دیتابیس وجود نداشت)
			var errors = string.Join(" | ", roleResult.Errors.Select(e => e.Description));
			// می‌توانید کاربر را پاک کنید یا خطا پرتاب کنید
			throw new ValidationException($"خطا در تخصیص نقش: {errors}");
		}

		// 6) تولید توکن‌ها:
		//    - Access Token از توکن‌سرویس T-021 که تاپل (Token, ExpiresAtUtc) برمی‌گرداند
		//    - Refresh Token تصادفی ۵۱۲ بیتی (توکن‌سرویس این متد را ندارد، پس اینجا تولید می‌شود)
		var (accessToken, accessTokenExpiresAtUtc) = _jwtTokenService.GenerateAccessToken(user);
		var refreshToken = GenerateSecureRefreshToken();

		var refreshTokenExpiresAtUtc = DateTime.UtcNow.Add(RefreshTokenLifetime);

		// 7) ذخیره Refresh Token در دیتابیس — فقط Hash (SHA-256)، نه Plain Text؛
		//    در صورت نشت دیتابیس، مهاجم نمی‌تواند توکن خام را استفاده کند.
		//    (Id و CreatedAt توسط RepositoryBase و TodoAppDbContext.BeforeSave پر می‌شوند)
		await _refreshTokenRepository.AddAsync(new RefreshToken
		{
			UserId = user.Id,
			TokenHash = Sha256Hex(refreshToken),
			ExpiresAtUtc = refreshTokenExpiresAtUtc
		}, cancellationToken);

		// 8) بازگرداندن نتیجه؛ تنظیم Cookie در Controller انجام می‌شود (بدون تغییر).
		return new AuthResponseDto
		{
			User = new UserDto { Id = user.Id, Email = user.Email! },
			AccessToken = accessToken,
			RefreshToken = refreshToken,
			AccessTokenExpiresAt = accessTokenExpiresAtUtc,
			RefreshTokenExpiresAt = refreshTokenExpiresAtUtc
		};
	}

	/// <summary>توکن تصادفی ۵۱۲ بیتی (Base64) برای Refresh Token</summary>
	private static string GenerateSecureRefreshToken()
		=> Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

	private static string Sha256Hex(string value)
		=> Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

	// هش ساختگی برای هم‌زمان‌سازی زمان پاسخ وقتی کاربر وجود ندارد (Timing Attack).
	// استاتیک است تا فقط یک‌بار ساخته شود و هزینه‌ای به هر Request تحمیل نکند.
	private static readonly string DummyBcryptHash =
		BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), workFactor: BcryptWorkFactor);

	public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken cancellationToken = default)
	{
		// ۱) یافتن کاربر با NormalizedEmail (مقایسه case-insensitive)
		var user = await _userManager.FindByEmailAsync(dto.Email);

		
		// ۲) Timing Attack — اگر کاربر وجود ندارد:
		//    دقیقاً همان عملیات Verify با همان work factor انجام می‌شود تا زمان پاسخ
		//    با حالت «رمز اشتباه» (~۲۵۰ms) یکسان باشد و وجود ایمیل قابل تشخیص نباشد.
		if (user is null)
		{
			BCrypt.Net.BCrypt.Verify(dto.Password, DummyBcryptHash);
			throw new InvalidCredentialsException(); // → 401 با پیام کلی
		}

		if (!user.IsActive)
		{
			throw new ValidationException("کاربر مورد نظر غیر فعال میباشد");
		}

		// ۳) Brute Force Protection — بررسی قفل بودن حساب، قبل از هر چیز:
		//    پیام عمداً نمی‌گوید چند دقیقه مانده (Information Leak).
		if (user.LockoutEnd is { } lockoutEnd && lockoutEnd > DateTimeOffset.UtcNow)
		{
			throw new LockedException(); // → 423
		}

		// ۴) بررسی رمز با BCrypt
		var isPasswordValid = !string.IsNullOrEmpty(user.PasswordHash)
			&& BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);

		if (!isPasswordValid)
		{
			// ۵) شمارش تلاش ناموفق
			user.FailedLoginAttempts++;

			if (user.FailedLoginAttempts >= MaxFailedLoginAttempts)
			{
				user.LockoutEnd = DateTimeOffset.UtcNow.Add(LockoutDuration);

				// ریست شمارنده هنگام اعمال قفل: پس از پایان قفل، کاربر دوباره ۵ تلاش کامل دارد.
				// در غیر این صورت یک تلاشِ ناموفق بلافاصله قفل بعدی می‌ساخت و حساب
				// عملاً با یک تلاش در هر ۱۵ دقیقه برای همیشه قابل قفل‌ماندن بود (DoS روی کاربر واقعی).
				user.FailedLoginAttempts = 0;
			}

			await _userManager.UpdateAsync(user);

			// پیام کلی — افشا نمی‌کند رمز اشتباه بود یا ایمیل وجود ندارد (User Enumeration)
			throw new InvalidCredentialsException(); // → 401
		}

		// ۶) ورود موفق → ریست شمارنده و پاک‌کردن قفل
		//    مهم: اگر کاربر پس از ۴ تلاش ناموفق با رمز صحیح وارد شود، شمارش باید صفر شود.
		user.FailedLoginAttempts = 0;
		user.LockoutEnd = null;
		await _userManager.UpdateAsync(user);

		// ۷) تولید توکن‌ها — مشابه Register (متدهای private در بخش Register تعریف شده‌اند)
		var (accessToken, accessTokenExpiresAtUtc) = _jwtTokenService.GenerateAccessToken(user);
		var refreshToken = GenerateSecureRefreshToken();
		var refreshTokenExpiresAtUtc = DateTime.UtcNow.Add(RefreshTokenLifetime);

		// ۸) ذخیره Refresh Token — فقط Hash (SHA-256)، نه Plain Text
		await _refreshTokenRepository.AddAsync(new RefreshToken
		{
			UserId = user.Id,
			TokenHash = Sha256Hex(refreshToken),
			ExpiresAtUtc = refreshTokenExpiresAtUtc
		}, cancellationToken);

		// ۹) تنظیم Cookie در Controller انجام می‌شود
		return new AuthResponseDto
		{
			User = new UserDto { Id = user.Id, Email = user.Email! },
			AccessToken = accessToken,
			RefreshToken = refreshToken,
			AccessTokenExpiresAt = accessTokenExpiresAtUtc,
			RefreshTokenExpiresAt = refreshTokenExpiresAtUtc
		};
	}
	/// <summary>
	/// چرخش توکن: با Refresh Token معتبر، Access Token و Refresh Token جدید صادر می‌شود.
	/// هر خطا → 401 با پیام کلی (یافت نشد / منقضی / قبلاً استفاده شده تفکیک نمی‌شود — Information Leak).
	/// </summary>
	public async Task<AuthResponseDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(refreshToken))
			throw new InvalidCredentialsException(); // → 401

		// ۱) ورودی کاربر Hash می‌شود و با Hash ذخیره‌شده در DB مقایسه می‌گردد (توکن خام در DB نیست)
		var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(Sha256Hex(refreshToken), cancellationToken);

		// ۲) یافت نشد / منقضی / IsRevoked (قبلاً استفاده شده — اثر Token Rotation) → 401
		if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAtUtc <= DateTime.UtcNow)
			throw new InvalidCredentialsException(); // → 401

		// ۳) صاحب توکن باید هنوز وجود داشته باشد
		var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
		if (user is null)
			throw new InvalidCredentialsException(); // → 401

		// ۴) توکن‌های جدید — GenerateAccessToken مقدار فعلی JwtVersion کاربر را در claim «jwtv» می‌گذارد
		var (accessToken, accessTokenExpiresAtUtc) = _jwtTokenService.GenerateAccessToken(user);
		var newRefreshToken = GenerateSecureRefreshToken();
		var refreshTokenExpiresAtUtc = DateTime.UtcNow.Add(RefreshTokenLifetime);

		// ۵) ابتدا ابطال توکن قبلی (اگر بین دو مرحله کرش شود، کاربر فقط دوباره لاگین می‌کند؛
		//    ولی اگر برعکس بود، دو توکن معتبر هم‌زمان می‌داشتیم — امنیت اول است)
		storedToken.IsRevoked = true;
		storedToken.RevokedAtUtc = DateTime.UtcNow;

		// ۶) درج توکن جدید — چون RepositoryBase با همان DbContext Scoped کار می‌کند و AddAsync
		//    SaveChanges انجام می‌دهد (مثل جریان Register)، ابطال قبلی هم اتمیک در همان SaveChanges ثبت می‌شود.
		var newStoredToken = new RefreshToken
		{
			UserId = user.Id,
			TokenHash = Sha256Hex(newRefreshToken),
			ExpiresAtUtc = refreshTokenExpiresAtUtc
		};
		storedToken.ReplacedByTokenId = newStoredToken.Id;

		await _refreshTokenRepository.AddAsync(newStoredToken, cancellationToken);

		return new AuthResponseDto
		{
			User = new UserDto { Id = user.Id, Email = user.Email! },
			AccessToken = accessToken,
			RefreshToken = newRefreshToken,
			AccessTokenExpiresAt = accessTokenExpiresAtUtc,
			RefreshTokenExpiresAt = refreshTokenExpiresAtUtc
		};
	}

	/// <summary>
	/// خروج کاربر: همه Refresh Token های فعال باطل می‌شوند (خروج از همه دستگاه‌ها) و
	/// JwtVersion افزایش می‌یابد تا تمام Access Token های موجود نیز باطل شوند.
	/// </summary>
	public async Task LogoutAsync(int userId, CancellationToken cancellationToken = default)
	{
		var user = await _userManager.FindByIdAsync(userId.ToString());
		if (user is null)
			throw new InvalidCredentialsException(); // → 401

		// ۱) ابتدا Refresh Token ها — اگر مرحله ۲ ناتمام بماند، بدترین حالت این است که
		//    Access Token ها تا انقضای کوتاه خود زنده بمانند ولی دیگر تمدید نشوند (قابل قبول).
		var activeTokens = await _refreshTokenRepository.GetActiveByUserIdAsync(userId, cancellationToken);
		var now = DateTime.UtcNow;
		foreach (var token in activeTokens)
		{
			token.IsRevoked = true;
			token.RevokedAtUtc = now;
		}
		await _refreshTokenRepository.UpdateRangeAsync(activeTokens, cancellationToken);

		// ۲) افزایش JwtVersion → در اعتبارسنجی، توکن‌های قدیمی (با jwtv کوچک‌تر) رد می‌شوند
		user.JwtVersion++;
		await _userManager.UpdateAsync(user);
	}
}
