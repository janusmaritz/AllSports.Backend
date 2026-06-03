using AllSports.Application.Interfaces.Darts.Repository;
using AllSports.Application.Queries.Darts;
using AllSports.Domain.Entities.Darts;
using AllSports.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AllSports.Infrastructure.Repositories.Darts;

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
        return await _context.Players.AnyAsync(p => p.FullName.ToLower() == name.ToLower());
    }

    public async Task<(List<PlayerProfile> Items, int TotalCount)> GetPlayersAsync(PlayerQuery query)
    {
        var q = _context.Players.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLower();
            q = q.Where(p =>
                p.FullName.ToLower().Contains(term) ||
                p.Nickname.ToLower().Contains(term) ||
                p.DartsUsed.ToLower().Contains(term) ||
                p.WalkOnSong.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.DartBrand))
        {
            var brand = query.DartBrand.ToLower();
            q = q.Where(p =>
                (p.DartBrand != null && p.DartBrand.ToLower().Contains(brand)) ||
                (p.DartBrand == null && p.DartsUsed.ToLower().Contains(brand)));
        }

        if (!string.IsNullOrWhiteSpace(query.DartWeight))
        {
            var weight = query.DartWeight.ToLower();
            var weightNum = weight.TrimEnd('g');
            q = q.Where(p =>
                (p.DartWeight != null && p.DartWeight.ToLower() == weight) ||
                (p.DartWeight == null && (
                    p.DartsUsed.ToLower().StartsWith(weightNum + "g") ||
                    p.DartsUsed.ToLower().StartsWith(weightNum + " gram"))));
        }

        var totalCount = await q.CountAsync();

        q = query.SortBy?.ToLower() switch
        {
            "age"  => query.SortDescending ? q.OrderByDescending(p => p.Age)      : q.OrderBy(p => p.Age),
            _      => query.SortDescending ? q.OrderByDescending(p => p.FullName)  : q.OrderBy(p => p.FullName)
        };

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
