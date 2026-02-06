namespace AllSports.API.Requests;

public class BulkScrapeRequest
{
    public List<string> Urls { get; set; } = new();
}