using AllSports.Application.Queries.Darts;
using AllSports.Domain.Entities.Darts;

namespace AllSports.Application.Interfaces.Darts.Repository;

public interface IDartsTournamentRepository
{
    Task ReplaceTournamentsAsync(string sourceUrl, List<DartsTournament> tournaments);
    Task<(List<DartsTournament> Items, int TotalCount)> GetTournamentsAsync(TournamentQuery query);
}
