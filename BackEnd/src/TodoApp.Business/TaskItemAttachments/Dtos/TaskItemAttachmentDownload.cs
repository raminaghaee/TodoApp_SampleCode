namespace TodoApp.Business.TaskItemAttachments;

public sealed class TaskItemAttachmentDownload
{
	public string FileName { get; init; } = string.Empty;
	public string? MimeType { get; init; }
	public byte[] FileData { get; init; } = [];
}
