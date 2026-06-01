using AllSports.Application.Interfaces.Darts.Repository;
using AllSports.Application.Interfaces.Darts.Services;
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
        {
            throw new Exception("No rankings found.");
        }

        await _repo.ReplaceRankingsAsync(url, rankings);

        return rankings;
    }

    public async Task<List<DartsRanking>> GetAllRankingsAsync()
    {
        return await _repo.GetAllRankingsAsync();
    }
}
