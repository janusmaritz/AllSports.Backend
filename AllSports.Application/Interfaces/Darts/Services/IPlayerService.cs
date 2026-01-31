using AllSports.Application.Responses;
using AllSports.Domain.Entities.Darts;

namespace AllSports.Application.Interfaces.Darts.Services;

public interface IPlayerService
{
    Task<PlayerProfile> ImportPlayerFromUrlAsync(string url);

    Task<BulkImportResult> ImportPlayersAsync(List<string> urls);

    Task<List<PlayerProfile>> GetAllPlayersAsync();
}