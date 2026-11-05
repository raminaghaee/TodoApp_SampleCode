using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApp.API.Extensions;
using TodoApp.Business.TaskLists;

namespace TodoApp.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/lists")]
[Produces("application/json")]
public class TaskListsController : ControllerBase
{
	private readonly ITaskListService _listService;

	public TaskListsController(ITaskListService listService) => _listService = listService;

	[HttpGet]
	[ProducesResponseType(typeof(IReadOnlyList<TaskListDto?>), StatusCodes.Status200OK)]
	public async Task<ActionResult<IReadOnlyList<TaskListDto?>>> GetMyLists(CancellationToken ct)
		=> Ok(await _listService.GetMyTaskListsAsync(User.GetUserId(), ct));

	[HttpPost]
	[ProducesResponseType(typeof(TaskListDto), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<TaskListDto>> Create([FromBody] CreateTaskListRequest request, CancellationToken ct)
	{
		var result = await _listService.CreateAsync(User.GetUserId(), request, ct);
		return Created($"/api/v1/lists/{result.Id}", result);
	}

	[HttpPatch("{id:int}")]
	[ProducesResponseType(typeof(TaskListDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<ActionResult<TaskListDto>> Update(int id, [FromBody] UpdateTaskListRequest request, CancellationToken ct)
		=> Ok(await _listService.UpdateAsync(User.GetUserId(), id, request, ct));

	[HttpDelete("{id:int}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	[ProducesResponseType(StatusCodes.Status403Forbidden)]
	public async Task<IActionResult> Delete(int id, CancellationToken ct)
	{
		await _listService.DeleteAsync(User.GetUserId(), id, ct);
		return NoContent();
	}

	[HttpPost("{id:int}/shares")]
	[ProducesResponseType(typeof(TaskListShareDto), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status409Conflict)]
	public async Task<ActionResult<TaskListShareDto>> CreateShare(int id, [FromBody] CreateTaskListShareRequest request, CancellationToken ct)
	{
		var result = await _listService.CreateShareAsync(User.GetUserId(), id, request, ct);
		return Created($"/api/v1/lists/{id}/shares/{result.UserId}", result);
	}

	[HttpDelete("{id:int}/shares/{userId:int}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> DeleteShare(int id, int userId, CancellationToken ct)
	{
		await _listService.DeleteShareAsync(User.GetUserId(), id, userId, ct);
		return NoContent();
	}

	/// <summary> برای نمایش نشان «اشتراکی» روی لیست‌ها.</summary>
	[HttpGet("~/api/v1/shares")]
	[ProducesResponseType(typeof(IReadOnlyList<TaskListShareDto>), StatusCodes.Status200OK)]
	public async Task<ActionResult<IReadOnlyList<TaskListShareDto>>> GetVisibleShares(CancellationToken ct)
		=> Ok(await _listService.GetVisibleTaskListSharesAsync(User.GetUserId(), ct));
}
