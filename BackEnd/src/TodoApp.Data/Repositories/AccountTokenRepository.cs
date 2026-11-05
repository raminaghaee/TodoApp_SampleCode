using Microsoft.EntityFrameworkCore;
using TodoApp.Business.Entites;
using TodoApp.Business.IRepositories;
using TodoApp.Data.Contexts;

namespace TodoApp.Data.Repositories;


public class AccountTokenRepository : IAccountTokenRepository
{
	#region Fields
	private readonly TodoAppDbContext _db;
	#endregion

	#region Ctors
	public AccountTokenRepository(TodoAppDbContext db) => _db = db;

	#endregion

	public async Task AddAsync(AccountToken token, CancellationToken ct = default)
	{
		_db.AccountTokens.Add(token);
		await _db.SaveChangesAsync(ct);
	}

	public async Task<AccountToken?> GetByTokenHashAsync(string tokenHash, string purpose, CancellationToken ct = default)
		=> await _db.AccountTokens
			.AsNoTracking()
			.FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.Purpose == purpose, ct);

	public Task InvalidatePendingAsync(int userId, string purpose, CancellationToken ct = default)
		=> _db.AccountTokens
			.Where(t => t.UserId == userId && t.Purpose == purpose && t.UsedAtUtc == null)
			.ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAtUtc, DateTime.UtcNow), ct);

	public Task<int> TryConsumeAsync(string tokenHash, string purpose, DateTime nowUtc, CancellationToken ct = default)
		=> _db.AccountTokens
			.Where(t => t.TokenHash == tokenHash
					 && t.Purpose == purpose
					 && t.UsedAtUtc == null
					 && t.ExpiresAtUtc > nowUtc)
			.ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAtUtc, nowUtc), ct);
}

