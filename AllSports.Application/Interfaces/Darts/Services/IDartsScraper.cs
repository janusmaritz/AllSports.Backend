using AllSports.Domain.Entities.Darts;

namespace AllSports.Application.Interfaces.Darts.Services;

public interface IDartsScraper
{
    Task<PlayerProfile?> ScrapePlayerAsync(string profileUrl);

    Task<List<DartsRanking>> ScrapeRankingsAsync(string rankingsUrl);

    Task<List<DartsTournament>> ScrapeTournamentsAsync(string calendarUrl);
}
