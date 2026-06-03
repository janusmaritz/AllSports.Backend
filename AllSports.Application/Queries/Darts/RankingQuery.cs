using AllSports.Application.Common.Pagination;

namespace AllSports.Application.Queries.Darts;

public class RankingQuery : PagedQuery
{
    public string? PlayerName { get; set; }
    public int? MinRank { get; set; }
    public int? MaxRank { get; set; }
}
