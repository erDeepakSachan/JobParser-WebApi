namespace JobParser.Infrastructure.OpenAI;

public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
    public string Model { get; set; } = "gpt-4.1-mini";
    public double Temperature { get; set; } = 0;
}