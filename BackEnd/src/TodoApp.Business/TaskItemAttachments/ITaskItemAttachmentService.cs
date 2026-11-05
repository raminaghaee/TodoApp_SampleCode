namespace TodoApp.Business.TaskItemAttachments;

public interface ITaskItemAttachmentService
{
	Task<IReadOnlyList<TaskItemAttachmentDto>> GetByTaskAsync(int userId, int taskId, CancellationToken ct = default);
	Task<TaskItemAttachmentDto> UploadAsync(int userId, int taskId, string? fileName, string? contentType, long contentLength, Stream content, CancellationToken ct = default);
	Task<TaskItemAttachmentDownload> DownloadAsync(int userId, int attachmentId, CancellationToken ct = default);
	Task DeleteAsync(int userId, int attachmentId, CancellationToken ct = default);
}
