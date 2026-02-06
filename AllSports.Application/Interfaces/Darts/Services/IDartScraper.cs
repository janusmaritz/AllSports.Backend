using AllSports.Domain.Entities.Darts;

namespace AllSports.Application.Interfaces.Darts.Services;

public interface IDartsScraper
{
    Task<PlayerProfile> ScrapePlayerAsync(string profileUrl);
}