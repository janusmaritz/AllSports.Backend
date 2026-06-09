using AllSports.Domain.Entities.Auth;

namespace AllSports.Application.Interfaces.Auth.Repository;

public interface IUserRepository
{
    Task<AppUser?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task AddAsync(AppUser user);
}
