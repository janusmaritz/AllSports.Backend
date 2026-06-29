using AllSports.Application.Responses.Auth;

namespace AllSports.Application.Interfaces.Auth.Services;

public interface IAuthService
{
    Task<AuthResult> LoginAsync(string email, string password);
    Task<AuthResult> RegisterAsync(string email, string password);
}
