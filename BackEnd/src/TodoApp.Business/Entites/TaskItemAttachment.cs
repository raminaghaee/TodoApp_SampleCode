using TodoApp.Business.Common;

namespace TodoApp.Business.Entites;

public class TaskItemAttachment : BaseEntity,ICurrentUserService
{
	#region Props
	/// <summary>مسیر منطقی فایل (db://{guid}/{fileName})</summary>
	public string StoragePath { get; set; } = string.Empty;
	public string FileName { get; set; } = string.Empty;
	public string? MimeType { get; set; }
	/// <summary>حداکثر ۲۰ مگابایت</summary>
	public long? SizeBytes { get; set; }
	/// <summary>محتوای باینری فایل در دیتابیس</summary>
	public byte[] FileData { get; set; } = [];
	public int TaskItemId { get; set; }
	public int UserId { get; set; } 
	#endregion
}
