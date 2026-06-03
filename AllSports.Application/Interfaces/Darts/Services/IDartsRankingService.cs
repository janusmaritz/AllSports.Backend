using AllSports.Application.Common.Pagination;
using AllSports.Application.Queries.Darts;
using AllSports.Domain.Entities.Darts;

namespace AllSports.Application.Interfaces.Darts.Services;

public interface IDartsRankingService
{
    Task<List<DartsRanking>> ImportRankingsFromUrlAsync(string url);
    Task<PagedResult<DartsRanking>> GetRankingsAsync(RankingQuery query);
}
