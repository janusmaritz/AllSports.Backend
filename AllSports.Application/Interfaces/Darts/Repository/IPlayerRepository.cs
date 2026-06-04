using AllSports.Application.Queries.Darts;
using AllSports.Domain.Entities.Darts;

namespace AllSports.Application.Interfaces.Darts.Repository;

public interface IPlayerRepository
{
    Task<int> AddPlayerAsync(PlayerProfile player);
    Task<PlayerProfile?> GetPlayerByIdAsync(int id);
    Task<bool> PlayerExistsAsync(string name);
    Task<(List<PlayerProfile> Items, int TotalCount)> GetPlayersAsync(PlayerQuery query);
}