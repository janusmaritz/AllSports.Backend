using AllSports.Application.Common.Pagination;
using AllSports.Application.Interfaces.Darts.Repository;
using AllSports.Application.Interfaces.Darts.Services;
using AllSports.Application.Queries.Darts;
using AllSports.Domain.Entities.Darts;

namespace AllSports.Application.Services.Darts;

public class DartsRankingService : IDartsRankingService
{
    private readonly IDartsScraper _scraper;
    private readonly IDartsRankingRepository _repo;

    public DartsRankingService(IDartsScraper scraper, IDartsRankingRepository repo)
    {
        _scraper = scraper;
        _repo = repo;
    }

    public async Task<List<DartsRanking>> ImportRankingsFromUrlAsync(string url)
    {
        var rankings = await _scraper.ScrapeRankingsAsync(url);
        if (rankings.Count == 0)
            throw new Exception("No rankings found.");

        await _repo.ReplaceRankingsAsync(url, rankings);

        return rankings;
    }

    public async Task<PagedResult<DartsRanking>> GetRankingsAsync(RankingQuery query)
    {
        query.Page = Math.Max(1, query.Page);
        query.PageSize = Math.Clamp(query.PageSize, 1, 100);

        var (items, totalCount) = await _repo.GetRankingsAsync(query);
        return PagedResult<DartsRanking>.From(items, totalCount, query.Page, query.PageSize);
    }
}
