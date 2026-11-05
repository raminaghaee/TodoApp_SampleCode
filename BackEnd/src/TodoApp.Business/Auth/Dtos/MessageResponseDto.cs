using System.Text.Json.Serialization;

namespace TodoApp.Business.Auth;

/// <summary>پاسخ استاندارد پیام‌محور (مثلاً «لینک تأیید ارسال شد.»).</summary>
public sealed class MessageResponseDto
{
	[JsonPropertyName("message")]
	public string Message { get; set; } = string.Empty;
}
