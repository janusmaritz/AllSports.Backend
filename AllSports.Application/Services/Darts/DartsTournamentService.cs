using AllSports.Application.Common.Pagination;
using AllSports.Application.Interfaces.Darts.Repository;
using AllSports.Application.Interfaces.Darts.Services;
using AllSports.Application.Queries.Darts;
using AllSports.Domain.Entities.Darts;

namespace AllSports.Application.Services.Darts;

public class DartsTournamentService : IDartsTournamentService
{
    private readonly IDartsScraper _scraper;
    private readonly IDartsTournamentRepository _repo;

    public DartsTournamentService(IDartsScraper scraper, IDartsTournamentRepository repo)
    {
        _scraper = scraper;
        _repo = repo;
    }

    public async Task<List<DartsTournament>> ImportTournamentsFromUrlAsync(string url)
    {
        var tournaments = await _scraper.ScrapeTournamentsAsync(url);
        if (tournaments.Count == 0)
            throw new ArgumentException("No tournaments found at the provided URL.");

        await _repo.ReplaceTournamentsAsync(url, tournaments);

        return tournaments;
    }

    public async Task<PagedResult<DartsTournament>> GetTournamentsAsync(TournamentQuery query)
    {
        query.Page = Math.Max(1, query.Page);
        query.PageSize = Math.Clamp(query.PageSize, 1, 100);

        var (items, totalCount) = await _repo.GetTournamentsAsync(query);
        return PagedResult<DartsTournament>.From(items, totalCount, query.Page, query.PageSize);
    }
}
