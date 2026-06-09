using AllSports.Domain.Entities.Auth;

namespace AllSports.Application.Interfaces.Auth.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) GenerateToken(AppUser user);
}
