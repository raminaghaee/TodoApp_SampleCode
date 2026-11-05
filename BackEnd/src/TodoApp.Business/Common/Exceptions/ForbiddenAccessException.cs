namespace TodoApp.Business;

public class ForbiddenAccessException : Exception
{
	public ForbiddenAccessException(string message) : base(message) { }
}
