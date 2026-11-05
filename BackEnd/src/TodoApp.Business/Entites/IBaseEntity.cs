namespace TodoApp.Business;

public interface IBaseEntity : ISoftDeletable
{
	int Id { get; set; }
}
