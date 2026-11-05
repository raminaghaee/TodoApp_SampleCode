using System.Security.Claims;
using TodoApp.Business.Common;

namespace TodoApp.API.Services;

public class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
	public int UserId
	{
		get
		{
			// JwtBearer به‌صورت پیش‌فرض claim "sub" را به NameIdentifier map می‌کند؛
			// هر دو را چک می‌کنیم تا در برابر تنظیمات مختلف JWT مقاوم باشد.
			var value =
				accessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
				?? accessor.HttpContext?.User?.FindFirstValue("sub");

			return int.TryParse(value, out var id)
				? id
				: throw new UnauthorizedAccessException("User is not authenticated.");
		}
	}
}
