namespace AllSports.Domain.Entities.Darts;

public class PlayerProfile
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public int? Age { get; set; }
    public string DartsUsed { get; set; } = string.Empty;
    public string WalkOnSong { get; set; } = string.Empty;
}