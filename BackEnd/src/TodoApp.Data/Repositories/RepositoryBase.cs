using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using TodoApp.Business;
using TodoApp.Data.Contexts;

namespace TodoApp.Data;

public class RepositoryBase<TEntity> : IRepositoryBase<TEntity>
	   where TEntity : class, IBaseEntity
{
	#region fields
	private readonly TodoAppDbContext _db;
	private int maxId;
	private int MaxId
	{
		get
		{
			if (maxId == 0)
				maxId = Table().IgnoreQueryFilters().Max(x => (int?)x.Id) ?? 0;
			return maxId;
		}
		set { maxId = value; }
	}
	#endregion
	#region Ctors
	public RepositoryBase(TodoAppDbContext Db) => _db = Db;
	#endregion
	#region Manipulate
	#region Add
	public virtual async Task<bool> AddAsync(TEntity entity, CancellationToken ct)
	{
		_db.Entry(entity).State = EntityState.Added;
		return await SaveAsync(ct);
	}
	public virtual async Task<bool> AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct)
	{
		foreach (var entity in entities)
			_db.Entry(entity).State = EntityState.Added;
		return await SaveAsync(ct);
	}
	#endregion
	#region Update
	public virtual async Task<bool> UpdateAsync(TEntity entity, CancellationToken ct)
	{
		_db.Entry(entity).State = EntityState.Modified;
		return await SaveAsync(ct);
	}
	public virtual async Task<bool> UpdateRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct)
	{
		foreach (var entity in entities)
			_db.Entry(entity).State = EntityState.Modified;
		return await SaveAsync(ct);

	}
	/// <summary>
	/// بازیابی وظیفه حذف‌شده از Trash.
	/// ⚠ هر دو فیلد باید ریست شوند: IsDeleted=false و DeletedAt=null.
	/// اگر فقط IsDeleted=false شود و DeletedAt بماند، Cleanup Job (T-027)
	/// ممکن است وظیفه بازیابی‌شده را به‌اشتباه Hard Delete کند.
	/// </summary>
	public async Task<bool> RestoreAsync(TEntity entity, CancellationToken ct = default)
	{
		if (entity is null)
			return false;

		entity.IsDeleted = false;
		entity.DeletedAt = null;

		return await SaveAsync(ct);
	}
	#endregion
	#region Delete
	/// <summary>
	/// Delete from the database
	/// </summary>
	/// <param name="entity"></param>
	/// <returns></returns>
	public virtual async Task<bool> DeleteAsync(TEntity entity, CancellationToken ct)
	{
		_db.Entry(entity).State = EntityState.Deleted;
		return await SaveAsync(ct);
	}
	public virtual async Task<bool> DeleteAsync(int id, CancellationToken ct)
	{
		var entity = await GetByIdAsync(id, ct);
		return entity is null ? throw new NotFoundException("تسک", id) : await DeleteAsync(entity, ct);
	}
	public virtual async Task<bool> DeleteRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct)
	{
		foreach (var entity in entities)
			_db.Entry(entity).State = EntityState.Deleted;
		return await SaveAsync(ct);
	}
	#endregion
	#region SaveChange
	protected virtual async Task<bool> SaveAsync(CancellationToken ct)
	{
		try
		{
			var entities = _db.ChangeTracker.Entries().Where(p => p.State != EntityState.Unchanged);

			Parallel.ForEach(entities, entity =>
			{
				FillEntityProperty(entity);
			});

			var result = await _db.SaveChangesAsync(ct);
			return result > 0 ? true : throw new Exception("خطا غیر منتظره  در ذخیره سازی دیتا رخ داد");
		}
		catch (Exception ex)
		{
			throw new Exception(ex.Message);
		}
	}
	private void SetID(EntityEntry entity)
	{
		if (entity.Entity is IBaseEntity item)
		{
			item.Id = ++MaxId;
		}
	}
	private void FillEntityProperty(EntityEntry entity)
	{
		if (entity.State == EntityState.Added)
		{
			SetID(entity);
		}
		else if (entity.State == EntityState.Modified)
		{
		}
	} 
	#endregion
	#endregion
	#region Get
	public virtual async Task<TEntity?> GetByIdAsync(int id, CancellationToken ct=default)
		=> await _db.Set<TEntity>().FindAsync(new object[] { id }, ct);

	public IQueryable<TEntity> Table()
		=> _db.Set<TEntity>();

	public virtual IQueryable<TEntity> Table(string[] includeList)
	{
		try
		{
			var query = Table();
			foreach (var item in includeList)
			{
				query = query.Include(item);
			}
			return query;
		}
		catch (Exception ex)
		{
			throw new Exception(ex.Message);
		}
	}

	public IQueryable<TEntity> TableAsNoTracking()
		=> _db.Set<TEntity>().AsNoTracking();

	public async Task<IEnumerable<TEntity>> GetAllAsync()
		=> await Table().ToListAsync();

	public virtual async Task<IEnumerable<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> experssion,CancellationToken ct)
		=> await Table().Where(experssion).ToListAsync(ct);

	public virtual async Task<IEnumerable<TEntity>> GetAllAsNoTrackingAsync()
		=> await TableAsNoTracking().ToListAsync();

	public virtual async Task<IEnumerable<TEntity>> GetAllAsNoTrackingAsync(Expression<Func<TEntity, bool>> experssion)
		=> await TableAsNoTracking().Where(experssion).ToListAsync();

	public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> experssion, CancellationToken ct)
		=> await Table().AnyAsync(experssion, ct);
	public bool Any(Expression<Func<TEntity, bool>> experssion)
		=> Table().Any();
	public virtual async Task<TEntity> GetFirstAsync(Expression<Func<TEntity, bool>> experssion, CancellationToken ct)
		=> await Table().FirstAsync(experssion, ct);

	public virtual async Task<TEntity?> GetFirstOrDefaultAsync(Expression<Func<TEntity, bool>> experssion, CancellationToken ct)
		=> await Table().FirstOrDefaultAsync(experssion, ct);

	#endregion
}
