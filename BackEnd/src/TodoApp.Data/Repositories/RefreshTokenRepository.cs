using Microsoft.EntityFrameworkCore;
using TodoApp.Business.Entites;
using TodoApp.Business.IRepositories;
using TodoApp.Data.Contexts;

namespace TodoApp.Data.Repositories;

public class RefreshTokenRepository : RepositoryBase<RefreshToken>, IRefreshTokenRepository
{
	#region Ctors
	public RefreshTokenRepository(TodoAppDbContext db) : base(db) { } 
	#endregion

	/// <summary>بدون AsNoTracking — entity باید Tracked بماند تا IsRevoked قابل تغییر باشد</summary>
	public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default)
		=> GetFirstOrDefaultAsync(x => x.TokenHash == tokenHash, ct);

	public Task<List<RefreshToken>> GetActiveByUserIdAsync(int userId, CancellationToken ct = default)
		=>	Table()
			.Where(x => x.UserId == userId && !x.IsRevoked)
			.ToListAsync(ct);
}
