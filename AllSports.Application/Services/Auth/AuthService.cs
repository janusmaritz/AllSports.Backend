using AllSports.Application.Interfaces.Auth.Repository;
using AllSports.Application.Interfaces.Auth.Services;
using AllSports.Application.Responses.Auth;
using AllSports.Domain.Entities.Auth;

namespace AllSports.Application.Services.Auth;

public class AuthService(IUserRepository users, IPasswordHasher hasher, ITokenService tokenService) : IAuthService
{
    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var user = await users.GetByEmailAsync(email.ToLowerInvariant())
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!hasher.Verify(user.PasswordHash, password))
            throw new UnauthorizedAccessException("Invalid credentials.");

        return BuildResult(user);
    }

    public async Task<AuthResult> RegisterAsync(string email, string password)
    {
        var normalizedEmail = email.ToLowerInvariant();

        if (await users.EmailExistsAsync(normalizedEmail))
            throw new InvalidOperationException("Email is already registered.");

        var user = new AppUser
        {
            Email = normalizedEmail,
            PasswordHash = hasher.Hash(password),
            Role = "User",
            CreatedAtUtc = DateTime.UtcNow,
        };

        await users.AddAsync(user);
        return BuildResult(user);
    }

    private AuthResult BuildResult(AppUser user)
    {
        var (token, expiresAt) = tokenService.GenerateToken(user);
        return new AuthResult
        {
            Token = token,
            ExpiresAt = expiresAt,
            Email = user.Email,
            Role = user.Role,
        };
    }
}
