using System.Net;
using System.Text;
using AllSports.Application.Common.Pagination;
using AllSports.Application.Interfaces.Darts.Repository;
using AllSports.Application.Interfaces.Darts.Services;
using AllSports.Application.Queries.Darts;
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

    // Decodes HTML entities (e.g. &eacute; → é) and normalizes Unicode to NFC
    // so that composed and decomposed forms of the same character compare equal.
    private static string NormalizeString(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        return WebUtility.HtmlDecode(value).Normalize(NormalizationForm.FormC).Trim();
    }

    private static void NormalizeProfile(PlayerProfile profile)
    {
        profile.FullName   = NormalizeString(profile.FullName);
        profile.Nickname   = NormalizeString(profile.Nickname);
        profile.DartsUsed  = NormalizeString(profile.DartsUsed);
        profile.WalkOnSong = NormalizeString(profile.WalkOnSong);
        profile.DartBrand  = profile.DartBrand  is null ? null : NormalizeString(profile.DartBrand);
        profile.DartWeight = profile.DartWeight is null ? null : NormalizeString(profile.DartWeight);
    }

    public async Task<PlayerProfile> ImportPlayerFromUrlAsync(string url)
    {
        var profile = await _scraper.ScrapePlayerAsync(url);
        if (profile == null) throw new Exception("Player not found.");

        NormalizeProfile(profile);

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

    public async Task<PagedResult<PlayerProfile>> GetPlayersAsync(PlayerQuery query)
    {
        query.Page = Math.Max(1, query.Page);
        query.PageSize = Math.Clamp(query.PageSize, 1, 100);

        var (items, totalCount) = await _repo.GetPlayersAsync(query);
        return PagedResult<PlayerProfile>.From(items, totalCount, query.Page, query.PageSize);
    }
}