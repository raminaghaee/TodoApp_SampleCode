using Microsoft.EntityFrameworkCore.Storage;
using TodoApp.Business.Common;
using TodoApp.Data.Contexts;

namespace TodoApp.Data.Common;

public class UnitOfWork : IUnitOfWork
{
	private readonly TodoAppDbContext _db;
	private IDbContextTransaction? _transaction;

    public UnitOfWork(TodoAppDbContext db) => _db = db;

    public async Task BeginTransactionAsync(CancellationToken ct = default)
	{
		if (_transaction is not null)
			return; 

		_transaction = await _db.Database.BeginTransactionAsync(ct);
	}

	public async Task CommitAsync(CancellationToken ct = default)
	{
		if (_transaction is null)
			return;

		await _db.Database.CommitTransactionAsync(ct);
		await _transaction.DisposeAsync();
		_transaction = null;
	}

	public async Task RollbackAsync(CancellationToken ct = default)
	{
		if (_transaction is null)
			return;

		await _db.Database.RollbackTransactionAsync(ct);
		await _transaction.DisposeAsync();
		_transaction = null;
	}
}
