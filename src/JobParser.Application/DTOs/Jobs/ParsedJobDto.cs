using System.Text.Json.Serialization;

namespace JobParser.Application.DTOs.Jobs;

public sealed class ParsedJobDto
{
    [JsonPropertyName("CompanyName")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("JobLocation")]
    public string? JobLocation { get; set; }

    [JsonPropertyName("Qualification")]
    public string? Qualification { get; set; }

    [JsonPropertyName("Department")]
    public string? Department { get; set; }

    [JsonPropertyName("IsITJob")]
    public bool? IsITJob { get; set; }

    [JsonPropertyName("InterviewDate")]
    public string? InterviewDate { get; set; }

    [JsonPropertyName("InterviewTime")]
    public string? InterviewTime { get; set; }

    [JsonPropertyName("InterviewLocation")]
    public string? InterviewLocation { get; set; }

    [JsonPropertyName("ContactNumber")]
    public string? ContactNumber { get; set; }

    [JsonPropertyName("Email")]
    public string? Email { get; set; }

    [JsonPropertyName("OtherDetail")]
    public string? OtherDetail { get; set; }
}