using AllSports.Domain.Entities.Darts;

namespace AllSports.Application.Interfaces.Darts.Services;

public interface IDartsRankingService
{
    Task<List<DartsRanking>> ImportRankingsFromUrlAsync(string url);
    Task<List<DartsRanking>> GetAllRankingsAsync();
}
