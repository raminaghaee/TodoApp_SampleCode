using TodoApp.Business.Entites;
using TodoApp.Business.IRepositories;
using TodoApp.Data.Contexts;

namespace TodoApp.Data.Repositories;

public class SubtaskRepository : RepositoryBase<Subtask>, ISubtaskRepository
{
	#region Ctors
	public SubtaskRepository(TodoAppDbContext Db) : base(Db) { }
	#endregion
}
