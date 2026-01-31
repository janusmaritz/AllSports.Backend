namespace AllSports.Domain.Entities.Darts;

public class DartsMatch
{
    public int Id { get; set; }
    public string TournamentName { get; set; } = string.Empty;
    public DateTime MatchDate { get; set; }

    // Competitors
    public string PlayerOneName { get; set; } = string.Empty;
    public string PlayerTwoName { get; set; } = string.Empty;

    // Results (Nullable because a scheduled match might not have a score yet)
    public int? PlayerOneScore { get; set; }
    public int? PlayerTwoScore { get; set; }

    public bool IsFinished { get; set; }
}
