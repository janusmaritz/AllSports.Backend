namespace AllSports.Application.Responses;

public class BulkImportResult
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> Errors { get; set; } = new();
}