using AllSports.Application.Common.Pagination;
using AllSports.Application.Queries.Darts;
using AllSports.Domain.Entities.Darts;

namespace AllSports.Application.Interfaces.Darts.Services;

public interface IDartsTournamentService
{
    Task<List<DartsTournament>> ImportTournamentsFromUrlAsync(string url);
    Task<PagedResult<DartsTournament>> GetTournamentsAsync(TournamentQuery query);
}
