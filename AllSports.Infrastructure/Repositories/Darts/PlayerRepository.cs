using AllSports.Application.Interfaces.Darts.Repository;
using AllSports.Domain.Entities.Darts;
using Microsoft.EntityFrameworkCore;
using AllSports.Infrastructure.Persistence;

namespace MyProject.Infrastructure.Repositories;

public class PlayerRepository : IPlayerRepository
{
    private readonly ApplicationDbContext _context;

    public PlayerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> AddPlayerAsync(PlayerProfile player)
    {
        _context.Players.Add(player);
        await _context.SaveChangesAsync();
        return player.Id;
    }

    public async Task<bool> PlayerExistsAsync(string name)
    {
        return await _context.Players.AnyAsync(p => p.FullName == name);
    }

    public async Task<List<PlayerProfile>> GetAllPlayersAsync()
    {
        return await _context.Players.ToListAsync();
    }
}