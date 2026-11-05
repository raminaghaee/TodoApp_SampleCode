using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApp.API.Extensions;
using TodoApp.Business.Admin;

namespace TodoApp.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/admin/users")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
	private readonly IAdminService _adminService;

	public AdminController(IAdminService adminService) => _adminService = adminService;

	[HttpGet]
	[ProducesResponseType(typeof(IReadOnlyList<AdminUserDto>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> GetAll(CancellationToken ct)
	{
		await _adminService.EnsureSuperAdminAsync(User.GetUserId(), ct);
		return Ok(await _adminService.GetAllUsersAsync(ct));
	}

	[HttpPatch("{id:int}")]
	[ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<AdminUserDto>> SetActive(int id, [FromBody] SetActiveRequest request, CancellationToken ct)
	{
		await _adminService.EnsureSuperAdminAsync(User.GetUserId(), ct);
		return Ok(await _adminService.SetActiveAsync(id, request.IsActive, ct));
	}

	[HttpPut("{id:int}/role")]
	[ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> SetRole(int id, [FromBody] SetRoleRequest request, CancellationToken ct)
	{
		await _adminService.EnsureSuperAdminAsync(User.GetUserId(), ct);
		await _adminService.SetRoleAsync(id, request.Role, ct);
		return Ok(new { ok = true });
	}
}
