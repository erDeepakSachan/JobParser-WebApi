using FluentAssertions;
using JobParser.Application.DTOs.Jobs;
using JobParser.Application.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace JobParser.Tests;

public class JobDetailMapperTests
{
    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Processing:DefaultInterviewTime"] = "08:00 AM",
                ["Processing:DefaultInterviewLocation"] = "Please call to given number to know the address"
            })
            .Build();

    [Fact]
    public void Map_Should_Fallback_InterviewTime_And_Email_Null()
    {
        var mapper = new JobDetailMapper(BuildConfig());

        var dto = new ParsedJobDto
        {
            CompanyName = "ABC Pvt Ltd",
            InterviewDate = null,
            InterviewTime = null,
            Email = null,
            IsITJob = false
        };

        var result = mapper.Map(dto, "ABC Pvt Ltd, 9876543210");

        result.InterviewTime.Should().Be("08:00 AM");
        result.Email.Should().BeNull();
        result.ContactNumber.Should().Be("9876543210");
    }

    [Fact]
    public void Map_Should_Use_Default_InterviewLocation_When_Not_Found()
    {
        var mapper = new JobDetailMapper(BuildConfig());

        var dto = new ParsedJobDto
        {
            CompanyName = "Starion",
            IsITJob = false
        };

        var result = mapper.Map(dto, "Starion job post without address");

        result.InterviewLocation.Should().Be("Please call to given number to know the address");
    }

    [Fact]
    public void Map_Should_Detect_IsItJob_From_Text()
    {
        var mapper = new JobDetailMapper(BuildConfig());

        var dto = new ParsedJobDto();

        var result = mapper.Map(dto, "We are hiring software developer with C# and SQL skills");

        result.IsITJob.Should().BeTrue();
    }
}