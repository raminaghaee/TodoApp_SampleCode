using TodoApp.Business.Entites;
using TodoApp.Business.IRepositories;
using TodoApp.Data.Contexts;

namespace TodoApp.Data.Repositories;

public class TaskItemAttachmentRepository : RepositoryBase<TaskItemAttachment>, ITaskItemAttachmentRepository
{
	#region Ctors
	public TaskItemAttachmentRepository(TodoAppDbContext Db) : base(Db) { }
	#endregion
}
