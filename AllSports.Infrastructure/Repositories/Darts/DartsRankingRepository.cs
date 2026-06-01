using AllSports.Application.Interfaces.Darts.Repository;
using AllSports.Domain.Entities.Darts;
using AllSports.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AllSports.Infrastructure.Repositories.Darts;

public class DartsRankingRepository : IDartsRankingRepository
{
    private readonly ApplicationDbContext _context;

    public DartsRankingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task ReplaceRankingsAsync(string sourceUrl, List<DartsRanking> rankings)
    {
        var existingRankings = await _context.DartsRankings
            .Where(r => r.SourceUrl == sourceUrl)
            .ToListAsync();

        _context.DartsRankings.RemoveRange(existingRankings);
        _context.DartsRankings.AddRange(rankings);

        await _context.SaveChangesAsync();
    }

    public async Task<List<DartsRanking>> GetAllRankingsAsync()
    {
        return await _context.DartsRankings
            .OrderBy(r => r.Rank)
            .ToListAsync();
    }
}
