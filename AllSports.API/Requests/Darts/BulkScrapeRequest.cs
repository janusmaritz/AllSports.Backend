namespace AllSports.API.Requests.Darts;

public class BulkScrapeRequest
{
    public List<string> Urls { get; set; } = new();
}