namespace TodoApp.Business;


/// <summary>400 — رمز فعلی درست نیست (در change-password و change-email)</summary>
public class CurrentPasswordInvalidException : Exception
{
	public CurrentPasswordInvalidException() : base("رمز فعلی اشتباه است.") { }
}
