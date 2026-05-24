namespace JobParser.Domain.Entities;

public sealed class JobDetail
{
    public string? CompanyName { get; set; }
    public string? JobLocation { get; set; }
    public string? Qualification { get; set; }
    public string? Department { get; set; }
    public bool IsITJob { get; set; }
    public DateTime? InterviewDate { get; set; }
    public string InterviewTime { get; set; } = "08:00 AM";
    public string InterviewLocation { get; set; } = "Please call to given number to know the address";
    public string? ContactNumber { get; set; }
    public string? Email { get; set; }
    public string OtherDetail { get; set; } = string.Empty;
}