using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JobParser.Application.DTOs.Jobs;
using JobParser.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobParser.Infrastructure.OpenAI;

public sealed class OpenAiChatClient : IJobParsingService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiChatClient> _logger;

    public OpenAiChatClient(HttpClient httpClient, IOptions<OpenAiOptions> options, ILogger<OpenAiChatClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ParsedJobDto>> ParseAsync(string rawMessage, CancellationToken cancellationToken)
    {
        var primaryContent = await CallOpenAiAsync(rawMessage, cancellationToken);

        if (OpenAiJsonParser.TryParseJobs(primaryContent, out var jobs))
            return jobs;

        _logger.LogWarning("OpenAI returned malformed JSON. Attempting repair.");

        var repairPrompt = """
The previous assistant output was malformed or not strict JSON.

Fix it and return ONLY valid JSON matching:
{{
  "jobs": [
    {{
      "CompanyName": "string",
      "JobLocation": "string or null",
      "Qualification": "string or null",
      "Department": "string or null",
      "IsITJob": true,
      "InterviewDate": "yyyy-MM-dd or null",
      "InterviewTime": "string",
      "InterviewLocation": "string",
      "ContactNumber": "string or null",
      "Email": "string or null",
      "OtherDetail": "string"
    }}
  ]
}}

Malformed output:
<<<BEGIN_OUTPUT
{primaryContent}
END_OUTPUT>>>
""";

        var repaired = await CallOpenAiRepairAsync(repairPrompt, cancellationToken);

        if (OpenAiJsonParser.TryParseJobs(repaired, out jobs))
            return jobs;

        _logger.LogError("OpenAI repair also failed. Raw response: {Response}", repaired);
        throw new InvalidOperationException("OpenAI response could not be parsed into valid JSON.");
    }

    private async Task<string> CallOpenAiAsync(string rawMessage, CancellationToken cancellationToken)
    {
        var body = new OpenAiChatRequest
        {
            Model = _options.Model,
            Temperature = _options.Temperature,
            MaxTokens = 3000,
            ResponseFormat = new { type = "json_object" },
            Messages = new List<OpenAiMessage>
            {
                new() { Role = "system", Content = OpenAiPromptBuilder.BuildSystemPrompt() },
                new() { Role = "user", Content = OpenAiPromptBuilder.BuildUserPrompt(rawMessage) }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OpenAI API failed. Status={Status}, Body={Body}", response.StatusCode, responseContent);
            throw new HttpRequestException($"OpenAI call failed with status {response.StatusCode}");
        }

        var chatResponse = JsonSerializer.Deserialize<OpenAiChatResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var content = chatResponse?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("OpenAI returned empty content.");

        return content;
    }

    private async Task<string> CallOpenAiRepairAsync(string repairPrompt, CancellationToken cancellationToken)
    {
        var body = new OpenAiChatRequest
        {
            Model = _options.Model,
            Temperature = 0,
            MaxTokens = 3000,
            ResponseFormat = new { type = "json_object" },
            Messages = new List<OpenAiMessage>
            {
                new() { Role = "system", Content = "You repair malformed JSON. Return only strict JSON." },
                new() { Role = "user", Content = repairPrompt }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("OpenAI repair API failed. Status={Status}, Body={Body}", response.StatusCode, responseContent);
            throw new HttpRequestException($"OpenAI repair call failed with status {response.StatusCode}");
        }

        var chatResponse = JsonSerializer.Deserialize<OpenAiChatResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return chatResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? string.Empty;
    }
}