using FluentAssertions;
using JobParser.Infrastructure.OpenAI;
using Xunit;

namespace JobParser.Tests;

public class OpenAiJsonParserTests
{
    [Fact]
    public void TryParseJobs_Should_Parse_Wrapper_Json()
    {
        var json = """
        {
          "jobs": [
            {
              "CompanyName": "ABC",
              "JobLocation": "Noida",
              "Qualification": "10th",
              "Department": "Manufacturing",
              "IsITJob": false,
              "InterviewDate": "2026-05-22",
              "InterviewTime": "8:00 AM",
              "InterviewLocation": "Noida",
              "ContactNumber": "9999999999",
              "Email": null,
              "OtherDetail": "test"
            }
          ]
        }
        """;

        var ok = OpenAiJsonParser.TryParseJobs(json, out var jobs);

        ok.Should().BeTrue();
        jobs.Should().HaveCount(1);
        jobs[0].CompanyName.Should().Be("ABC");
    }

    [Fact]
    public void TryParseJobs_Should_Parse_Bare_Array_Json()
    {
        var json = """
        [
          {
            "CompanyName": "ABC",
            "JobLocation": "Noida"
          }
        ]
        """;

        var ok = OpenAiJsonParser.TryParseJobs(json, out var jobs);

        ok.Should().BeTrue();
        jobs.Should().HaveCount(1);
    }

    [Fact]
    public void ExtractJsonBlock_Should_Remove_Markdown_Fences()
    {
        var content = """
        ```json
        { "jobs": [] }
        ```
        """;

        var extracted = OpenAiJsonParser.ExtractJsonBlock(content);

        extracted.Should().Contain("\"jobs\"");
    }
}