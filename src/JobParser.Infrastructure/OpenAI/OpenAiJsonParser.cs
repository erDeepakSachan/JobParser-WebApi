using System.Text.Json;
using JobParser.Application.DTOs.Jobs;

namespace JobParser.Infrastructure.OpenAI;

public static class OpenAiJsonParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool TryParseJobs(string content, out IReadOnlyList<ParsedJobDto> jobs)
    {
        jobs = Array.Empty<ParsedJobDto>();

        if (string.IsNullOrWhiteSpace(content))
            return false;

        var json = ExtractJsonBlock(content);
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            var envelope = JsonSerializer.Deserialize<ParsedJobsEnvelopeDto>(json, Options);
            if (envelope?.Jobs is { Count: > 0 })
            {
                jobs = envelope.Jobs;
                return true;
            }
        }
        catch
        {
        }

        try
        {
            var array = JsonSerializer.Deserialize<List<ParsedJobDto>>(json, Options);
            if (array is { Count: > 0 })
            {
                jobs = array;
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    public static string ExtractJsonBlock(string content)
    {
        var trimmed = content.Trim();

        if (trimmed.StartsWith("```"))
        {
            var start = trimmed.IndexOf('\n');
            var end = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (start >= 0 && end > start)
                trimmed = trimmed[(start + 1)..end].Trim();
        }

        var firstObj = trimmed.IndexOf('{');
        var lastObj = trimmed.LastIndexOf('}');
        var firstArr = trimmed.IndexOf('[');
        var lastArr = trimmed.LastIndexOf(']');

        if (firstObj >= 0 && lastObj > firstObj)
            return trimmed.Substring(firstObj, lastObj - firstObj + 1);

        if (firstArr >= 0 && lastArr > firstArr)
            return trimmed.Substring(firstArr, lastArr - firstArr + 1);

        return trimmed;
    }
}