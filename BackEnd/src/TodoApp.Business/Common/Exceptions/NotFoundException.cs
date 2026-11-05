namespace TodoApp.Business;

public class NotFoundException : Exception
{
	public NotFoundException(string message) : base(message) { }
	public NotFoundException(string name, object key)
		: base($"موجودیت \"{name}\" با کد رایانه  ({key}) یافت نشد.") { }
}
