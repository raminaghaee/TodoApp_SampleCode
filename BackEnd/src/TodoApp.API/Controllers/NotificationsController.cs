using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApp.API.Extensions;
using TodoApp.Business.Notifications;

namespace TodoApp.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/notifications")]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
	private readonly INotificationService _notificationService;

	public NotificationsController(INotificationService notificationService) => _notificationService = notificationService;

	[HttpGet]
	[ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
	public async Task<ActionResult<IReadOnlyList<NotificationDto>>> GetMine(CancellationToken ct)
		=> Ok(await _notificationService.GetMineAsync(User.GetUserId(), ct));

	[HttpPost]
	[ProducesResponseType(typeof(NotificationDto), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<NotificationDto>> Create([FromBody] CreateNotificationRequest request, CancellationToken ct)
	{
		var result = await _notificationService.CreateAsync(User.GetUserId(), request, ct);
		return Created($"/api/v1/notifications/{result.Id}", result);
	}

	[HttpPost("read-all")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	public async Task<IActionResult> ReadAll(CancellationToken ct)
	{
		await _notificationService.MarkAllAsReadAsync(User.GetUserId(), ct);
		return NoContent();
	}

	[HttpPatch("{id:int}/read")]
	[ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<NotificationDto>> MarkAsRead(int id, CancellationToken ct)
		=> Ok(await _notificationService.MarkAsReadAsync(User.GetUserId(), id, ct));

	[HttpDelete("{id:int}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete(int id, CancellationToken ct)
	{
		await _notificationService.DeleteAsync(User.GetUserId(), id, ct);
		return NoContent();
	}
}
