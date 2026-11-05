namespace TodoApp.Business.Entites;

public class RefreshToken : BaseEntity
{
	#region Props
	public int UserId { get; set; }
	public string TokenHash { get; set; } = string.Empty;
	public DateTime ExpiresAtUtc { get; set; }
	/// <summary>با Token Rotation یا Logout مقدار true می‌گیرد</summary>
	public bool IsRevoked { get; set; }
	public DateTime? RevokedAtUtc { get; set; }
	public int? ReplacedByTokenId { get; set; }
	#endregion
	#region Navigations
	public AppUser User { get; set; } = null!;
	public RefreshToken? ReplacedByToken { get; set; } 
	#endregion
}
