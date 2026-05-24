using System.Text.Json.Serialization;

namespace JobParser.Application.DTOs.Jobs;

public sealed class ParsedJobsEnvelopeDto
{
    [JsonPropertyName("jobs")]
    public List<ParsedJobDto> Jobs { get; set; } = new();
}