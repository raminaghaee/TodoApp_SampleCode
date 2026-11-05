using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApp.API.Extensions;
using TodoApp.Business.TaskItems;

namespace TodoApp.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/tasks")]
[Produces("application/json")]
public class TaskItemsController : ControllerBase
{
	private readonly ITaskItemService _taskService;

	public TaskItemsController(ITaskItemService taskService) => _taskService = taskService;

	[HttpGet]
	[ProducesResponseType(typeof(IReadOnlyList<TaskItemDto>), StatusCodes.Status200OK)]
	public async Task<ActionResult<IReadOnlyList<TaskItemDto>>> GetVisible(CancellationToken ct)
		=> Ok(await _taskService.GetVisibleAsync(User.GetUserId(), ct));

	[HttpPost]
	[ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<TaskItemDto>> Create([FromBody] CreateTaskItemRequest request, CancellationToken ct)
	{
		var result = await _taskService.CreateAsync(User.GetUserId(), request, ct);
		return Created($"/api/v1/tasks/{result.Id}", result);
	}

	[HttpPatch("{id:int}")]
	[ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<TaskItemDto>> Update(int id, [FromBody] UpdateTaskItemRequest request, CancellationToken ct)
		=> Ok(await _taskService.UpdateAsync(User.GetUserId(), id, request, ct));

	[HttpDelete("{id:int}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete(int id, CancellationToken ct)
	{
		await _taskService.DeleteAsync(User.GetUserId(), id, ct);
		return NoContent();
	}
}

