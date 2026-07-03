using AllSports.Application.Interfaces.Darts.Repository;
using AllSports.Application.Queries.Darts;
using AllSports.Domain.Entities.Darts;
using AllSports.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AllSports.Infrastructure.Repositories.Darts;

public class DartsTournamentRepository : IDartsTournamentRepository
{
    private readonly ApplicationDbContext _context;

    public DartsTournamentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task ReplaceTournamentsAsync(string sourceUrl, List<DartsTournament> tournaments)
    {
        var existingTournaments = await _context.DartsTournaments
            .Where(t => t.SourceUrl == sourceUrl)
            .ToListAsync();

        _context.DartsTournaments.RemoveRange(existingTournaments);
        _context.DartsTournaments.AddRange(tournaments);

        await _context.SaveChangesAsync();
    }

    public async Task<(List<DartsTournament> Items, int TotalCount)> GetTournamentsAsync(TournamentQuery query)
    {
        var q = _context.DartsTournaments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLower();
            q = q.Where(t => t.Name.ToLower().Contains(term) || t.Location.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Organisation))
            q = q.Where(t => t.Organisation.ToLower() == query.Organisation.ToLower());

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            q = query.Status.ToLower() switch
            {
                "upcoming"  => q.Where(t => t.StartDate > today),
                "live"      => q.Where(t => t.StartDate <= today && t.EndDate >= today),
                "completed" => q.Where(t => t.EndDate < today),
                _           => q
            };
        }

        var totalCount = await q.CountAsync();

        q = query.SortBy?.ToLower() switch
        {
            "name"         => query.SortDescending ? q.OrderByDescending(t => t.Name)         : q.OrderBy(t => t.Name),
            "location"     => query.SortDescending ? q.OrderByDescending(t => t.Location)     : q.OrderBy(t => t.Location),
            "organisation" => query.SortDescending ? q.OrderByDescending(t => t.Organisation) : q.OrderBy(t => t.Organisation),
            _              => query.SortDescending
                                ? q.OrderByDescending(t => t.StartDate).ThenByDescending(t => t.Name)
                                : q.OrderBy(t => t.StartDate).ThenBy(t => t.Name)
        };

        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
