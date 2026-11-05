using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApp.API.Extensions;
using TodoApp.Business.Subtasks;

namespace TodoApp.API.Controllers;

[Authorize]
[ApiController]
[Produces("application/json")]
public class SubtasksController : ControllerBase
{
	private readonly ISubtaskService _subtaskService;

	public SubtasksController(ISubtaskService subtaskService) => _subtaskService = subtaskService;

	[HttpGet("api/v1/tasks/{taskId:int}/subtasks")]
	[ProducesResponseType(typeof(IReadOnlyList<SubtaskDto>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<IReadOnlyList<SubtaskDto>>> GetByTask(int taskId, CancellationToken ct)
		=> Ok(await _subtaskService.GetByTaskAsync(User.GetUserId(), taskId, ct));

	[HttpPost("api/v1/tasks/{taskId:int}/subtasks")]
	[ProducesResponseType(typeof(SubtaskDto), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<SubtaskDto>> Create(int taskId, [FromBody] CreateSubtaskRequest request, CancellationToken ct)
	{
		var result = await _subtaskService.CreateAsync(User.GetUserId(), taskId, request, ct);
		return Created($"/api/v1/tasks/{taskId}/subtasks/{result.Id}", result);
	}

	[HttpPatch("api/v1/subtasks/{id:int}")]
	[ProducesResponseType(typeof(SubtaskDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<SubtaskDto>> Update(int id, [FromBody] UpdateSubtaskRequest request, CancellationToken ct)
		=> Ok(await _subtaskService.UpdateAsync(User.GetUserId(), id, request, ct));

	[HttpDelete("api/v1/subtasks/{id:int}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete(int id, CancellationToken ct)
	{
		await _subtaskService.DeleteAsync(User.GetUserId(), id, ct);
		return NoContent();
	}
}

