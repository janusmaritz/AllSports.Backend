using AllSports.Application.Common.Pagination;

namespace AllSports.Application.Queries.Darts;

public class TournamentQuery : PagedQuery
{
    public string? SearchTerm { get; set; }
    public string? Organisation { get; set; }
    public string? Status { get; set; }
}
