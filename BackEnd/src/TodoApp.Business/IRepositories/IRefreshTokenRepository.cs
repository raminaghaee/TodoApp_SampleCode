using TodoApp.Business.Entites;

namespace TodoApp.Business.IRepositories;

public interface IRefreshTokenRepository : IRepositoryBase<RefreshToken>
{
	Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
	Task<List<RefreshToken>> GetActiveByUserIdAsync(int userId, CancellationToken ct = default);
}
