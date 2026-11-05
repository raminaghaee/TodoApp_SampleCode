using TodoApp.Business.Entites;
using TodoApp.Business.IRepositories;
using TodoApp.Data.Contexts;

namespace TodoApp.Data.Repositories;

public class TaskListShareRepository : RepositoryBase<TaskListShare>, ITaskListShareRepository
{
	#region Ctors
	public TaskListShareRepository(TodoAppDbContext Db) : base(Db) { }
	#endregion
}
