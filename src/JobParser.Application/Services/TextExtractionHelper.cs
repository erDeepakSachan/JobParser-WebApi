using System.Globalization;
using System.Text.RegularExpressions;

namespace JobParser.Application.Services;

public static class TextExtractionHelper
{
    private static readonly Regex UrlRegex = new(@"https?://[^\s]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new(
        @"(?<!\d)(?:\+?91[-\s]?)?[6-9]\d{9}(?!\d)|(?<!\d)\d{10}(?!\d)",
        RegexOptions.Compiled);

    private static readonly Regex EmailRegex = new(
        @"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DateRegex = new(
        @"\b(\d{1,2}[/-]\d{1,2}[/-]\d{2,4})\b",
        RegexOptions.Compiled);

    private static readonly Regex GoogleMapsUrlRegex = new(
        @"https?://(www\.)?(maps\.app\.goo\.gl|goo\.gl/maps|maps\.google\.com|www\.google\.com/maps)[^\s]*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string? ExtractFirstPhoneNumber(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var match = PhoneRegex.Match(text);
        if (!match.Success) return null;

        var value = match.Value.Trim();
        return value.Replace(" ", "").Replace("-", "");
    }

    public static string? ExtractEmail(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var match = EmailRegex.Match(text);
        return match.Success ? match.Value.Trim() : null;
    }

    public static DateTime? ExtractDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var normalized = value.Trim();

        var formats = new[]
        {
            "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy", "d/M/yyyy",
            "dd-MM-yyyy", "d-MM-yyyy", "dd-M-yyyy", "d-M-yyyy",
            "yyyy-MM-dd"
        };

        if (DateTime.TryParseExact(normalized, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt.Date;

        if (DateRegex.IsMatch(normalized))
        {
            var raw = DateRegex.Match(normalized).Groups[1].Value;
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt))
                return dt.Date;
        }

        if (DateTime.TryParse(normalized, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt))
            return dt.Date;

        return null;
    }

    public static string? ExtractAddressLikeLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var lines = text.Split('\n')
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToList();

        foreach (var line in lines)
        {
            var lower = line.ToLowerInvariant();

            if (lower.StartsWith("add") ||
                lower.StartsWith("address") ||
                lower.Contains("address") ||
                lower.Contains("लोकेशन") ||
                lower.Contains("location") ||
                lower.Contains("वर्क लोकेशन") ||
                lower.Contains("venue"))
            {
                var cleaned = line
                    .Replace("ADD:-", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("ADD:", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("Address:", "", StringComparison.OrdinalIgnoreCase)
                    .Replace("Location:", "", StringComparison.OrdinalIgnoreCase)
                    .Trim(' ', '-', ':');

                if (!string.IsNullOrWhiteSpace(cleaned))
                    return cleaned;
            }
        }

        return null;
    }

    public static string BuildOtherDetail(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return string.Empty;

        var text = rawText.Replace("\r", "\n");

        text = GoogleMapsUrlRegex.Replace(text, match => $" __KEEP__{match.Value}__KEEP__ ");
        text = UrlRegex.Replace(text, string.Empty);
        text = text.Replace("__KEEP__", string.Empty);

        text = Regex.Replace(text, @"[ \t]+", " ");
        text = Regex.Replace(text, @"\n{2,}", "\n");
        text = text.Replace("\n", ", ");
        text = Regex.Replace(text, @"\s*,\s*", ", ");

        return text.Trim(' ', ',', ';');
    }

    public static bool DetectIsItJob(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText)) return false;

        var text = rawText.ToLowerInvariant();

        string[] itKeywords =
        [
            "software", "developer", "programmer", "coding", "python", "java",
            "dotnet", ".net", "asp.net", "c#", "frontend", "backend", "full stack",
            "sql", "database", "devops", "network", "it", "computer", "support engineer",
            "tester", "qa", "cloud", "sap", "erp", "hardware", "desktop support"
        ];

        string[] nonItKeywords =
        [
            "manufacturing", "factory", "operator", "helper", "production",
            "packing", "welding", "machine", "assembler", "technician"
        ];

        if (itKeywords.Any(k => text.Contains(k)))
            return true;

        if (nonItKeywords.Any(k => text.Contains(k)))
            return false;

        return false;
    }

    public static string? ExtractCompanyNameFromFirstLines(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText)) return null;

        var lines = rawText.Split('\n')
                           .Select(l => l.Trim())
                           .Where(l => !string.IsNullOrWhiteSpace(l))
                           .Take(6)
                           .ToList();

        foreach (var line in lines)
        {
            if (line.Contains("LTD", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("PVT", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("INDIA", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("COMPANY", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("PVT LTD", StringComparison.OrdinalIgnoreCase))
            {
                return line.Trim('!', '.', ':', '-', ' ');
            }
        }

        return null;
    }
}