namespace AllSports.Domain.Entities.Darts;

public class DartsRanking
{
    public int Id { get; set; }
    public int Rank { get; set; }
    public string PlayerName { get; set; } = string.Empty;
    public decimal MoneyAmount { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public DateTime ScrapedAtUtc { get; set; }
}
