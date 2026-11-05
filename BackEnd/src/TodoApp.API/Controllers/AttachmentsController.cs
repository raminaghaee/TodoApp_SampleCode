using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApp.API.Extensions;
using TodoApp.Business.TaskItemAttachments;

namespace TodoApp.API.Controllers;

[Authorize]
[ApiController]
[Produces("application/json")]
public class AttachmentsController : ControllerBase
{
	private readonly ITaskItemAttachmentService _attachmentService;

	public AttachmentsController(ITaskItemAttachmentService attachmentService) => _attachmentService = attachmentService;

	[HttpGet("api/v1/tasks/{taskId:int}/attachments")]
	[ProducesResponseType(typeof(IReadOnlyList<TaskItemAttachmentDto>), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<IReadOnlyList<TaskItemAttachmentDto>>> GetByTask(int taskId, CancellationToken ct)
		=> Ok(await _attachmentService.GetByTaskAsync(User.GetUserId(), taskId, ct));

	[HttpPost("api/v1/tasks/{taskId:int}/attachments")]
	[Consumes("multipart/form-data")]
	// 21MB برای عبور راحت فرم و هدرها + سقف ۲۰MB داخل سرویس چک می‌شود
	[RequestSizeLimit(21 * 1024 * 1024)]
	[ProducesResponseType(typeof(TaskItemAttachmentDto), StatusCodes.Status201Created)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<ActionResult<TaskItemAttachmentDto>> Upload(int taskId, IFormFile? file, CancellationToken ct)
	{
		if (file is null || file.Length == 0)
			return BadRequest(new { message = "فایلی ارسال نشده است." });

		await using var stream = file.OpenReadStream();
		var result = await _attachmentService.UploadAsync(
			User.GetUserId(), taskId, file.FileName, file.ContentType, file.Length, stream, ct);

		return Created($"/api/v1/attachments/{result.Id}/download", result);
	}

	[HttpGet("api/v1/attachments/{id:int}/download")]
	[ProducesResponseType(StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Download(int id, CancellationToken ct)
	{
		var file = await _attachmentService.DownloadAsync(User.GetUserId(), id, ct);
		return File(
			file.FileData,
			file.MimeType ?? "application/octet-stream",
			fileDownloadName: file.FileName,
			enableRangeProcessing: true);
	}

	[HttpDelete("api/v1/attachments/{id:int}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete(int id, CancellationToken ct)
	{
		await _attachmentService.DeleteAsync(User.GetUserId(), id, ct);
		return NoContent();
	}
}

