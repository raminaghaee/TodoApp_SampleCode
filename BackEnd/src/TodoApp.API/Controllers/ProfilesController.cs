using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApp.API.Extensions;
using TodoApp.Business.AccountService;
using TodoApp.Business.Auth;
using TodoApp.Business.Profiles;

namespace TodoApp.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/profiles")]
[Produces("application/json")]
public class ProfilesController : ControllerBase
{
	private readonly IProfileService _profileService;
	private readonly AccountService _accountService;

	private readonly IValidator<UpdateProfileDto> _updateProfileValidator;

	public ProfilesController(IProfileService profileService, IValidator<UpdateProfileDto> updateProfileValidator, AccountService accountService)
	{
		_profileService = profileService;
		_updateProfileValidator = updateProfileValidator;
		_accountService = accountService;
	}

	[HttpGet]
	[ProducesResponseType(typeof(IReadOnlyList<ProfileDto>), StatusCodes.Status200OK)]
	public async Task<ActionResult<IReadOnlyList<ProfileDto>>> GetRelated(CancellationToken ct)
		=> Ok(await _profileService.GetRelatedAsync(User.GetUserId(), ct));

	[HttpGet("lookup")]
	[ProducesResponseType(typeof(ProfileDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<IReadOnlyList<ProfileDto>>> Lookup([FromQuery] string? email, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(email))
		{
			return Ok(new List<ProfileDto>()); // بدون هدر رفتن به دیتابیس
		}
		return Ok(await _profileService.LookupByEmailAsync(email, ct));
	}

	[HttpPut("me")]
	[ProducesResponseType(typeof(ProfileUserDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status401Unauthorized)]
	public async Task<IActionResult> UpdateMe([FromBody] UpdateProfileDto dto, CancellationToken ct)
	{
		var validationResult = await _updateProfileValidator.ValidateAsync(dto, ct);
		if (!validationResult.IsValid)
		{
			foreach (var error in validationResult.Errors)
				ModelState.AddModelError(error.PropertyName, error.ErrorMessage);

			return ValidationProblem(ModelState);
		}

		var userId = User.GetUserId();
		var user = await _accountService.UpdateDisplayNameAsync(userId, dto.DisplayName);

		return Ok(user); // ProfileUserDto با فیلدهای snake_case قرارداد
	}
	//public async Task<ActionResult<ProfileDto>> UpdateMe([FromBody] UpdateProfileRequest request, CancellationToken ct)
	//	=> Ok(await _profileService.UpdateMeAsync(User.GetUserId(), request, ct));
}
