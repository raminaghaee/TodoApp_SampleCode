using TodoApp.Business.Entites;

namespace TodoApp.Business.Authentication;

public interface IJwtTokenService
{
	(string Token, DateTime ExpiresAtUtc) GenerateAccessToken(AppUser user);
}
