using TodoApp.Business.Entites;
using TodoApp.Business.IRepositories;
using TodoApp.Data.Contexts;

namespace TodoApp.Data.Repositories;

public class NotificationRepository : RepositoryBase<Notification>, INotificationRepository
{
	#region Ctors
	public NotificationRepository(TodoAppDbContext Db) : base(Db) { }
	#endregion
}
