using AllSports.Application.Interfaces.Darts.Repository;
using AllSports.Application.Interfaces.Darts.Services;
using AllSports.Application.Responses;
using AllSports.Domain.Entities.Darts;

namespace AllSports.Application.Services.Darts;

public class PlayerService : IPlayerService
{
    private readonly IDartsScraper _scraper;
    private readonly IPlayerRepository _repo;

    public PlayerService(IDartsScraper scraper, IPlayerRepository repo)
    {
        _scraper = scraper;
        _repo = repo;
    }

    public async Task<PlayerProfile> ImportPlayerFromUrlAsync(string url)
    {
        var profile = await _scraper.ScrapePlayerAsync(url);
        if (profile == null) throw new Exception("Player not found.");

        if (await _repo.PlayerExistsAsync(profile.FullName))
        {
            throw new InvalidOperationException($"Player '{profile.FullName}' already exists.");
        }

        await _repo.AddPlayerAsync(profile);
        return profile;
    }

    public async Task<BulkImportResult> ImportPlayersAsync(List<string> urls)
    {
        var result = new BulkImportResult();

        foreach (var url in urls)
        {
            try
            {
                await ImportPlayerFromUrlAsync(url);

                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailureCount++;
                result.Errors.Add($"Failed to import {url}: {ex.Message}");
            }
        }

        return result;
    }
}