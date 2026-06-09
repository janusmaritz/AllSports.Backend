using AllSports.Application.Interfaces.Auth.Repository;
using AllSports.Domain.Entities.Auth;
using AllSports.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AllSports.Infrastructure.Repositories.Auth;

public class UserRepository(ApplicationDbContext db) : IUserRepository
{
    public async Task<AppUser?> GetByEmailAsync(string email) =>
        await db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<bool> EmailExistsAsync(string email) =>
        await db.Users.AnyAsync(u => u.Email == email);

    public async Task AddAsync(AppUser user)
    {
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();
    }
}
