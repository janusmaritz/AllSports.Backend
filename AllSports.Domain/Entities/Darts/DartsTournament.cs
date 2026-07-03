namespace AllSports.Domain.Entities.Darts;

public class DartsTournament
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Organisation { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string DetailUrl { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public DateTime ScrapedAtUtc { get; set; }
}
