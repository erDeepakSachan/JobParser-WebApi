namespace JobParser.Application.DTOs.Jobs;

public sealed class WebhookProcessingResultDto
{
    public bool Success { get; set; }
    public long UpdateId { get; set; }
    public int ParsedJobs { get; set; }
    public int InsertedJobs { get; set; }
    public string Message { get; set; } = string.Empty;
    public IReadOnlyList<ParsedJobDto> Jobs { get; set; } = Array.Empty<ParsedJobDto>();
}