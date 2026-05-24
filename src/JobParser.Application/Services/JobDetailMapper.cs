using JobParser.Application.DTOs.Jobs;
using JobParser.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace JobParser.Application.Services;

public sealed class JobDetailMapper
{
    private readonly string _defaultInterviewTime;
    private readonly string _defaultInterviewLocation;

    public JobDetailMapper(IConfiguration configuration)
    {
        _defaultInterviewTime = configuration["Processing:DefaultInterviewTime"] ?? "08:00 AM";
        _defaultInterviewLocation = configuration["Processing:DefaultInterviewLocation"] ?? "Please call to given number to know the address";
    }

    public JobDetail Map(ParsedJobDto dto, string rawText)
    {
        var isItJob = dto.IsITJob ?? TextExtractionHelper.DetectIsItJob(rawText);

        return new JobDetail
        {
            CompanyName = FirstNonEmpty(dto.CompanyName, TextExtractionHelper.ExtractCompanyNameFromFirstLines(rawText)),
            JobLocation = FirstNonEmpty(dto.JobLocation),
            Qualification = FirstNonEmpty(dto.Qualification),
            Department = FirstNonEmpty(dto.Department, isItJob ? "IT" : "Manufacturing"),
            IsITJob = isItJob,
            InterviewDate = TextExtractionHelper.ExtractDate(dto.InterviewDate),
            InterviewTime = string.IsNullOrWhiteSpace(dto.InterviewTime) ? _defaultInterviewTime : dto.InterviewTime.Trim(),
            InterviewLocation = FirstNonEmpty(
                dto.InterviewLocation,
                TextExtractionHelper.ExtractAddressLikeLine(rawText),
                _defaultInterviewLocation),
            ContactNumber = FirstNonEmpty(dto.ContactNumber, TextExtractionHelper.ExtractFirstPhoneNumber(rawText)),
            Email = FirstNonEmpty(dto.Email, TextExtractionHelper.ExtractEmail(rawText)),
            OtherDetail = TextExtractionHelper.BuildOtherDetail(dto.OtherDetail ?? rawText)
        };
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
}