using System.Linq.Expressions;

namespace TodoApp.Business;

public interface IRepositoryBase<TEntity> where TEntity : class, IBaseEntity
{
	#region Manipulate
	Task<bool> AddAsync(TEntity entity, CancellationToken ct);
	Task<bool> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct);
	Task<bool> UpdateAsync(TEntity entity, CancellationToken ct);
	Task<bool> UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct);
	Task<bool> RestoreAsync(TEntity entity, CancellationToken ct = default);
	/// <summary>
	/// Delete from the database
	/// </summary>
	/// <param name="entity"></param>
	/// <returns></returns>
	Task<bool> DeleteAsync(TEntity entity, CancellationToken ct);
	Task<bool> DeleteAsync(int id, CancellationToken ct);
	Task<bool> DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct);
	#endregion
	#region Get
	Task<TEntity?> GetByIdAsync(int id, CancellationToken ct);
	IQueryable<TEntity> Table();
	IQueryable<TEntity> Table(string[] includeList);
	IQueryable<TEntity> TableAsNoTracking();
	Task<IEnumerable<TEntity>> GetAllAsync();
	Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> experssion,CancellationToken ct);
	Task<IEnumerable<TEntity>> GetAllAsNoTrackingAsync();
	Task<bool> AnyAsync(Expression<Func<TEntity, bool>> experssion, CancellationToken ct);
	bool Any(Expression<Func<TEntity, bool>> experssion);
	Task<TEntity> GetFirstAsync(Expression<Func<TEntity, bool>> experssion, CancellationToken ct);
	Task<TEntity?> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> experssion,CancellationToken ct);
	#endregion
}
