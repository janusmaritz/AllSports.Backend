using AllSports.Application.Queries.Darts;
using AllSports.Domain.Entities.Darts;

namespace AllSports.Application.Interfaces.Darts.Repository;

public interface IDartsRankingRepository
{
    Task ReplaceRankingsAsync(string sourceUrl, List<DartsRanking> rankings);
    Task<(List<DartsRanking> Items, int TotalCount)> GetRankingsAsync(RankingQuery query);
}
