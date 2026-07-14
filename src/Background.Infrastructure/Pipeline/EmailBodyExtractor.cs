using System.Text.Json;

namespace Background.Infrastructure.Pipeline;

public class EmailBodyExtractor : IEmailBodyExtractor
{
    public string ExtractBody(string rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return string.Empty;

        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        if (!root.TryGetProperty("body", out var body))
            return string.Empty;

        if (body.TryGetProperty("isHtml", out var isHtmlProp) && isHtmlProp.GetBoolean())
        {
            if (body.TryGetProperty("html", out var htmlProp))
            {
                var html = htmlProp.GetString();
                if (!string.IsNullOrWhiteSpace(html))
                    return html;
            }
            
            if (body.TryGetProperty("htmlBody", out var htmlProp2))
            {
                var html = htmlProp2.GetString();
                if (!string.IsNullOrWhiteSpace(html))
                    return html;
            }
        }

        if (body.TryGetProperty("text", out var textProp))
        {
            return textProp.GetString() ?? string.Empty;
        }

        return string.Empty;
    }
}
