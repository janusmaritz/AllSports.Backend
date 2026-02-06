using AllSports.Domain.Entities.Darts;
using Microsoft.EntityFrameworkCore;

namespace AllSports.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<PlayerProfile> Players { get; set; }
}