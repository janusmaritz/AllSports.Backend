using AllSports.Domain.Entities.Darts;

namespace AllSports.Application.Interfaces.Darts.Repository;

public interface IPlayerRepository
{
    Task<int> AddPlayerAsync(PlayerProfile player);
    Task<bool> PlayerExistsAsync(string name);
}