using AllSports.Application.Common.Pagination;

namespace AllSports.Application.Queries.Darts;

public class PlayerQuery : PagedQuery
{
    public string? SearchTerm { get; set; }
    public string? DartBrand { get; set; }
    public string? DartWeight { get; set; }
}
