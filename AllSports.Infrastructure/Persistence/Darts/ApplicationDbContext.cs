using AllSports.Domain.Entities.Darts;
using Microsoft.EntityFrameworkCore;

namespace AllSports.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<PlayerProfile> Players { get; set; }
    public DbSet<DartsRanking> DartsRankings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DartsRanking>()
            .Property(r => r.MoneyAmount)
            .HasPrecision(18, 2);
    }
}
