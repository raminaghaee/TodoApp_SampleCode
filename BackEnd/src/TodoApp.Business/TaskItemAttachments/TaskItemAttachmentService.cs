using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TodoApp.Business.Entites;
using TodoApp.Business.IRepositories;
using TodoApp.Business.TaskItems;

namespace TodoApp.Business.TaskItemAttachments;

public sealed class TaskItemAttachmentService : ITaskItemAttachmentService
{
	#region Fields
	/// <summary>حداکثر ۲۰ مگابایت — مطابق قرارداد.</summary>
	public const long MaxFileSizeBytes = 20 * 1024 * 1024;

	private readonly ITaskItemAttachmentRepository _taskItemAttachmentRepository;
	private readonly ITaskItemRepository _taskItemRepository;
	private readonly ITaskItemService _taskItemService;
	#endregion
	#region Ctors
	public TaskItemAttachmentService(ITaskItemAttachmentRepository taskItemAttachmentRepository, ITaskItemRepository taskItemRepository, ITaskItemService taskItemService)
	{
		_taskItemAttachmentRepository = taskItemAttachmentRepository;
		_taskItemRepository = taskItemRepository;
		_taskItemService = taskItemService;
	}
	#endregion
	#region Get
	public async Task<IReadOnlyList<TaskItemAttachmentDto>> GetByTaskAsync(int userId, int taskId, CancellationToken ct = default)
	{
		_ = await _taskItemRepository.GetTaskItemWithAccessAsync(userId, taskId, ct)
			?? throw new NotFoundException("کار یافت نشد.");

		var attachments = await _taskItemAttachmentRepository.Table()
			.Where(a => a.TaskItemId == taskId)
			.OrderBy(a => a.CreatedAt).ThenBy(a => a.Id)
			.ToListAsync(ct);

		return attachments.Select(ToDto).ToList();
	} 
	#endregion
	#region Manipulate
	public async Task<TaskItemAttachmentDto> UploadAsync(int userId, int taskId, string? fileName, string? contentType, long contentLength, Stream content, CancellationToken ct = default)
	{
		var task = await _taskItemRepository.GetTaskItemWithAccessAsync(userId, taskId, ct)
			?? throw new NotFoundException("کار یافت نشد.");

		if (!_taskItemService.CanModifyTaskItemAsync(userId, task))
			throw new ForbiddenAccessException("فقط سازنده یا مسئول کار (یا مالک لیست) می‌تواند آن را تغییر دهد.");

		if (content is null || (contentLength <= 0 && content.CanSeek && content.Length <= 0))
			throw new ValidationException("فایلی ارسال نشده است.");

		if (contentLength > MaxFileSizeBytes)
			throw new ValidationException("حجم فایل نباید بیشتر از ۲۰ مگابایت باشد.");

		await using var memory = new MemoryStream();
		await content.CopyToAsync(memory, ct);
		var bytes = memory.ToArray();

		if (bytes.Length == 0)
			throw new ValidationException("فایل ارسالی خالی است.");
		if (bytes.LongLength > MaxFileSizeBytes)
			throw new ValidationException("حجم فایل نباید بیشتر از ۲۰ مگابایت باشد.");

		var safeName = Path.GetFileName(fileName);
		if (string.IsNullOrWhiteSpace(safeName))
			safeName = $"file-{DateTime.UtcNow:yyyyMMddHHmmss}";

		var attachment = new TaskItemAttachment
		{
			TaskItemId = taskId,
			StoragePath = $"db://{Guid.NewGuid():N}/{safeName}",
			FileName = safeName,
			MimeType = string.IsNullOrWhiteSpace(contentType) ? null : contentType,
			SizeBytes = bytes.LongLength,
			FileData = bytes,
			UserId = userId,
			CreatedAt = DateTime.UtcNow
		};

		_ = await _taskItemAttachmentRepository.AddAsync(attachment, ct);
		return ToDto(attachment);
	}
	public async Task<TaskItemAttachmentDownload> DownloadAsync(int userId, int attachmentId, CancellationToken ct = default)
	{
		var attachment = await _taskItemAttachmentRepository.GetFirstOrDefaultAsync(a => a.Id == attachmentId, ct)
			?? throw new NotFoundException("پیوست یافت نشد.");

		_ = await _taskItemRepository.GetTaskItemWithAccessAsync(userId, attachment.TaskItemId, ct)
			?? throw new NotFoundException("پیوست یافت نشد.");

		return new TaskItemAttachmentDownload
		{
			FileName = attachment.FileName,
			MimeType = attachment.MimeType,
			FileData = attachment.FileData
		};
	}
	public async Task DeleteAsync(int userId, int attachmentId, CancellationToken ct = default)
	{
		var attachment = await _taskItemAttachmentRepository.GetFirstOrDefaultAsync(a => a.Id == attachmentId, ct)
			?? throw new NotFoundException("پیوست یافت نشد.");

		var task = await _taskItemRepository.GetTaskItemWithAccessAsync(userId, attachment.TaskItemId, ct)
			?? throw new NotFoundException("پیوست یافت نشد.");


		if (!_taskItemService.CanModifyTaskItemAsync(userId, task))
			throw new ForbiddenAccessException("فقط سازنده یا مسئول کار (یا مالک لیست) می‌تواند آن را تغییر دهد.");

		await _taskItemAttachmentRepository.DeleteAsync(attachment, ct);
	}

	#endregion
	private static TaskItemAttachmentDto ToDto(TaskItemAttachment a) => new()
	{
		Id = a.Id,
		TaskItemId = a.TaskItemId,
		StoragePath = a.StoragePath,
		FileName = a.FileName,
		MimeType = a.MimeType,
		SizeBytes = a.SizeBytes,
		UploadedBy = a.UserId,
		CreatedAt = a.CreatedAt
	};
}
