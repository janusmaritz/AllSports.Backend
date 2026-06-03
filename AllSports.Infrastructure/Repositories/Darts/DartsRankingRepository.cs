using AllSports.Application.Interfaces.Darts.Repository;
using AllSports.Application.Queries.Darts;
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

    public async Task<(List<DartsRanking> Items, int TotalCount)> GetRankingsAsync(RankingQuery query)
    {
        var q = _context.DartsRankings.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.PlayerName))
            q = q.Where(r => r.PlayerName.ToLower().Contains(query.PlayerName.ToLower()));
        if (query.MinRank.HasValue)
            q = q.Where(r => r.Rank >= query.MinRank.Value);
        if (query.MaxRank.HasValue)
            q = q.Where(r => r.Rank <= query.MaxRank.Value);

        var totalCount = await q.CountAsync();

        q = query.SortBy?.ToLower() switch
        {
            "name"  => query.SortDescending ? q.OrderByDescending(r => r.PlayerName)   : q.OrderBy(r => r.PlayerName),
            "money" => query.SortDescending ? q.OrderByDescending(r => r.MoneyAmount)   : q.OrderBy(r => r.MoneyAmount),
            _       => query.SortDescending ? q.OrderByDescending(r => r.Rank)          : q.OrderBy(r => r.Rank)
        };

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
