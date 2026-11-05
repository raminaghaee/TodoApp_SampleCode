
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TodoApp.API.Extensions;
using TodoApp.Business;
using TodoApp.Business.AccountService;
using TodoApp.Business.Auth;
using TodoApp.Business.Services;
using TodoApp.Business.Users;

namespace TodoApp.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
	private const string AccessTokenCookieName = "access_token";
	private const string RefreshTokenCookieName = "refresh_token";

	private readonly AuthService _authService;
	private readonly AccountService _accountService;
	private readonly IValidator<RegisterDto> _registerValidator;
	private readonly IValidator<LoginDto> _loginValidator;
	private readonly IValidator<ChangePasswordDto> _changePasswordValidator;
	private readonly IValidator<ChangeEmailDto> _changeEmailValidator;
	private readonly IValidator<ForgotPasswordDto> _forgotPasswordValidator;
	private readonly IValidator<ResetPasswordDto> _resetPasswordValidator;
	private readonly IValidator<ConfirmEmailDto> _confirmEmailValidator;
	private readonly IUserDirectory _userDirectory;

	public AuthController(AuthService authService,
						  AccountService accountService,
						  IValidator<RegisterDto> registerValidator,
						  IValidator<LoginDto> loginValidator,
						  IValidator<ChangePasswordDto> changePasswordValidator,
						  IValidator<ChangeEmailDto> changeEmailValidator,
						  IValidator<ForgotPasswordDto> forgotPasswordValidator,
						  IValidator<ResetPasswordDto> resetPasswordValidator,
						  IValidator<ConfirmEmailDto> confirmEmailValidator,
						  IUserDirectory userDirectory)
	{
		_authService = authService;
		_accountService = accountService;
		_registerValidator = registerValidator;
		_loginValidator = loginValidator;
		_changePasswordValidator = changePasswordValidator;
		_changeEmailValidator = changeEmailValidator;
		_forgotPasswordValidator = forgotPasswordValidator;
		_resetPasswordValidator = resetPasswordValidator;
		_confirmEmailValidator = confirmEmailValidator;
		_userDirectory = userDirectory;
	}
	private async Task<UserDto> BuildContractUserAsync(int userId, CancellationToken ct)
	{
		var row = await _userDirectory.GetByIdAsync(userId)
				?? throw new NotFoundException("کاربر یافت نشد.");

		var role = await _userDirectory.GetRoleAsync(userId, ct);

		return new UserDto
		{
			Id = row.Id,
			Email = row.Email,
			DisplayName = row.DisplayName,
			Role = role
		};
	}
	private void SetAuthCookies(AuthResponseDto result)
	{
		var accessCookieOptions = new CookieOptions
		{
			HttpOnly = true,
			Secure = true,
			SameSite = SameSiteMode.Strict,
			Expires = result.AccessTokenExpiresAt
		};
		Response.Cookies.Append(AccessTokenCookieName, result.AccessToken, accessCookieOptions);

		var refreshCookieOptions = new CookieOptions
		{
			HttpOnly = true,
			Secure = true,
			SameSite = SameSiteMode.Strict,
			Expires = result.RefreshTokenExpiresAt,
			Path = "/api/v1/auth"
		};
		Response.Cookies.Append(RefreshTokenCookieName, result.RefreshToken, refreshCookieOptions);
	}

	private void ClearAuthCookies()
	{
		// Path باید دقیقاً همان Path تنظیم‌شده هنگام Append باشد (مثل Logout فعلی)
		Response.Cookies.Delete(AccessTokenCookieName);
		Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = "/api/v1/auth" });
	}

	[HttpPost("register")]
	[AllowAnonymous]
	[ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<IActionResult> Register([FromBody] RegisterDto dto, CancellationToken ct)
	{
		var validationResult = await _registerValidator.ValidateAsync(dto, ct);
		if (!validationResult.IsValid)
		{
			foreach (var error in validationResult.Errors)
				ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

			return ValidationProblem(ModelState); // 400
		}

		var result = await _authService.RegisterAsync(dto, ct);

		// توکن‌ها فقط در HttpOnly Cookie — نه در Body (محافظت در برابر XSS)
		SetAuthCookies(result);
		await BuildContractUserAsync(result.User.Id, ct);

		//return Created($"/api/v1/users/{result.User.Id}", result.User);
		//فرانت این شکلی پاسخ رو دریافت میکنه که به نظر صحیح نیست
		return Ok(new AuthResponse { Token = result.AccessToken, User = result.User });
	}

	[HttpPost("login")]
	[AllowAnonymous]
	[EnableRateLimiting(RateLimitingExtensions.LoginPolicy)] // حداکثر ۵ درخواست در ۱۵ دقیقه به ازای هر IP
	[ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status423Locked)]
	public async Task<IActionResult> Login([FromBody] LoginDto dto, CancellationToken ct)
	{
		var validationResult = await _loginValidator.ValidateAsync(dto, ct);
		if (!validationResult.IsValid)
		{
			foreach (var error in validationResult.Errors)
				ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

			return ValidationProblem(ModelState);
		}

		var result = await _authService.LoginAsync(dto, ct);
		SetAuthCookies(result);

		await BuildContractUserAsync(result.User.Id, ct);

		return Ok(new AuthResponse { Token = result.AccessToken, User = result.User });
	}

	[HttpGet("me")]
	[Authorize]
	[ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Me(CancellationToken ct)
	{
		var userId = User.GetUserId();
		return Ok(await BuildContractUserAsync(userId, ct));
	}

	[HttpPost("refresh")]
	[AllowAnonymous] // Access Token منقضی است؛ ورود با Refresh Token انجام می‌شود
	[EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
	[ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Refresh(CancellationToken cancellationToken = default)
	{
		// توکن فقط از HttpOnly Cookie خوانده می‌شود — کلاینت هیچ مقداری نمی‌فرستد
		var refreshToken = Request.Cookies[RefreshTokenCookieName];

		if (string.IsNullOrWhiteSpace(refreshToken))
			throw new InvalidCredentialsException(); // → 401

		// توکن نامعتبر/منقضی/استفاده‌شده → 401 | Rotation داخل سرویس انجام می‌شود
		var result = await _authService.RefreshAsync(refreshToken, cancellationToken);

		// کوکی‌های جدید جایگزین می‌شوند (Rotation سمت کلاینت هم اعمال شد)
		SetAuthCookies(result);

		return Ok(result.User);
	}

	/// <summary>خروج — ابطال همه Refresh Token ها و همه Access Token ها </summary>
	[HttpPost("logout")]
	[Authorize]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> Logout(CancellationToken cancellationToken)
	{
		if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
			return Unauthorized();

		await _authService.LogoutAsync(userId, cancellationToken);

		ClearAuthCookies();

		return NoContent(); // 204
	}

	// ─────────────────────────────────────────────────────────────────────────
	// مسیرهای جدید حساب کاربری — مطابق قرارداد فرانت‌اند
	// ─────────────────────────────────────────────────────────────────────────

	/// <summary>
	/// تغییر رمز از پروفایل — [Authorize] و شناسه از هویت تأییدشده (نه بدنه).
	/// همه نشست‌های دیگر باطل می‌شوند و نشست جاری با کوکی‌های جدید حفظ می‌شود.
	/// </summary>
	[HttpPost("change-password")]
	[Authorize]
	[EnableRateLimiting(RateLimitingExtensions.AccountSensitivePolicy)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
	public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken ct)
	{
		var validationResult = await _changePasswordValidator.ValidateAsync(dto, ct);
		if (!validationResult.IsValid)
		{
			foreach (var error in validationResult.Errors)
				ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

			return ValidationProblem(ModelState);
		}

		var userId = User.GetUserId();

		try
		{
			var result = await _accountService.ChangePasswordAsync(userId, dto, ct);

			// حفظ نشست جاری: کوکی‌های جدید (توکن‌های قبلی همه باطل شده‌اند)
			SetAuthCookies(result);
		}
		catch (CurrentPasswordInvalidException ex)
		{
			// طبق قرارداد: 400 ProblemDetails با detail قابل‌نمایش
			return Problem(statusCode: StatusCodes.Status400BadRequest, detail: ex.Message);
		}

		return NoContent(); // 204
	}

	/// <summary>
	/// درخواست تغییر ایمیل — رمز فعلی بررسی می‌شود؛ ایمیل جدید تا تأیید Pending می‌ماند؛
	/// لینک به ایمیل جدید و هشدار به ایمیل قبلی می‌رود.
	/// </summary>
	[HttpPost("change-email")]
	[Authorize]
	[EnableRateLimiting(RateLimitingExtensions.AccountSensitivePolicy)]
	[ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
	[ProducesResponseType(StatusCodes.Status502BadGateway)]
	public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailDto dto, CancellationToken ct)
	{
		var validationResult = await _changeEmailValidator.ValidateAsync(dto, ct);
		if (!validationResult.IsValid)
		{
			foreach (var error in validationResult.Errors)
				ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

			return ValidationProblem(ModelState);
		}

		var userId = User.GetUserId();

		try
		{
			var message = await _accountService.ChangeEmailAsync(userId, dto, ct);
			return Ok(message);
		}
		catch (CurrentPasswordInvalidException ex)
		{
			return Problem(statusCode: StatusCodes.Status400BadRequest, detail: ex.Message);
		}
		catch (EmailDeliveryException ex)
		{
			return Problem(statusCode: StatusCodes.Status502BadGateway, detail: ex.Message);
		}
		// ConflictException (ایمیل تکراری) و ValidationException به هندلر سراسری فعلی شما می‌روند (409/400)
	}

	/// <summary>
	/// فراموشی رمز — [AllowAnonymous]؛ پاسخ همیشه عمومی و یکسان است (افشای وجود حساب ممنوع).
	/// </summary>
	[HttpPost("forgot-password")]
	[AllowAnonymous]
	[EnableRateLimiting(RateLimitingExtensions.AccountRecoveryPolicy)]
	[ProducesResponseType(typeof(MessageResponseDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
	[ProducesResponseType(StatusCodes.Status502BadGateway)]
	public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto, CancellationToken ct)
	{
		var validationResult = await _forgotPasswordValidator.ValidateAsync(dto, ct);
		if (!validationResult.IsValid)
		{
			foreach (var error in validationResult.Errors)
				ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

			return ValidationProblem(ModelState);
		}

		try
		{
			return Ok(await _accountService.ForgotPasswordAsync(dto, ct));
		}
		catch (EmailDeliveryException ex)
		{
			return Problem(statusCode: StatusCodes.Status502BadGateway, detail: ex.Message);
		}
	}

	/// <summary>
	/// بازنشانی رمز با توکن بازیابی — [AllowAnonymous]؛ فقط توکن معتبرِ purpose=recovery پذیرفته می‌شود؛
	/// رمز فعلی گرفته نمی‌شود؛ پس از موفقیت همه نشست‌ها باطل و کوکی‌ها پاک می‌شوند (بدون ورود خودکار).
	/// </summary>
	[HttpPost("reset-password")]
	[AllowAnonymous]
	[EnableRateLimiting(RateLimitingExtensions.AccountRecoveryPolicy)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
	public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto, CancellationToken ct)
	{
		var validationResult = await _resetPasswordValidator.ValidateAsync(dto, ct);
		if (!validationResult.IsValid)
		{
			foreach (var error in validationResult.Errors)
				ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

			return ValidationProblem(ModelState);
		}

		try
		{
			await _accountService.ResetPasswordAsync(dto, ct);
		}
		catch (InvalidTokenException ex)
		{
			return Problem(statusCode: StatusCodes.Status400BadRequest, detail: ex.Message);
		}

		// ابطال نشست‌ها در سرویس انجام شد؛ کوکی‌ها پاک می‌شوند و ورود خودکار انجام نمی‌شود
		ClearAuthCookies();

		return NoContent(); // 204
	}

	/// <summary>
	/// تأیید ایمیل جدید — [AllowAnonymous]؛ توکن یک‌بارمصرفِ purpose=email_change؛
	/// ایمیل هویت و پروفایل یکجا به‌روزرسانی می‌شود و همه نشست‌ها باطل و کوکی‌ها پاک می‌شوند.
	/// </summary>
	[HttpPost("confirm-email")]
	[AllowAnonymous]
	[EnableRateLimiting(RateLimitingExtensions.AccountRecoveryPolicy)]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	[ProducesResponseType(StatusCodes.Status429TooManyRequests)]
	public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailDto dto, CancellationToken ct)
	{
		var validationResult = await _confirmEmailValidator.ValidateAsync(dto, ct);
		if (!validationResult.IsValid)
		{
			foreach (var error in validationResult.Errors)
				ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

			return ValidationProblem(ModelState);
		}

		try
		{
			await _accountService.ConfirmEmailChangeAsync(dto, ct);
		}
		catch (InvalidTokenException ex)
		{
			return Problem(statusCode: StatusCodes.Status400BadRequest, detail: ex.Message);
		}
		// ConflictException (ایمیل همزمان گرفته شده) به هندلر سراسری فعلی شما می‌رود (409)

		// فقط در موفقیت: نشست‌ها باطل شده‌اند؛ کوکی‌ها پاک می‌شوند تا کاربر با ایمیل جدید وارد شود
		ClearAuthCookies();

		return NoContent(); // 204
	}
}
