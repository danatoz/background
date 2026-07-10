using System.Text.RegularExpressions;
using Background.Dal.Entities;
using Background.Infrastructure.Storage;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace Background.Infrastructure.Pipeline.Steps;

public sealed partial class PreprocessingStep : IProcessingStep
{
    private readonly IStorageService _storage;
    private readonly ILogger<PreprocessingStep> _logger;

    public PreprocessingStep(IStorageService storage, ILogger<PreprocessingStep> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public string StepName => "Preprocessing";

    public async Task<IProcessingStepResult> ExecuteAsync(
        InboxMessage message, PipelineContext context, CancellationToken ct)
    {
        try
        {
            var raw = context.RawContent;
            var cleaned = StripHtml(raw);
            cleaned = CollapseWhitespace(cleaned);
            context.PreprocessedContent = cleaned.Trim();

            var key = ArtifactPathBuilder.Preprocessed(context.ArtifactPrefix);
            await _storage.SaveAsync(key, context.PreprocessedContent, "text/markdown", ct);

            _logger.LogDebug("Saved preprocessed artifact for {MessageId}: {Key}", message.Id, key);
            return ProcessingStepResult.Done;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Preprocessing failed for {MessageId}", message.Id);
            return ProcessingStepResult.Fail(ex.Message);
        }
    }

    private static string StripHtml(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
            return string.Empty;

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        if (doc.ParseErrors.Any() || doc.DocumentNode.SelectNodes("//*") == null)
        {
            return html;
        }

        return doc.DocumentNode.InnerText;
    }

    private static string CollapseWhitespace(string input)
    {
        return CollapseRegex().Replace(input, " ");
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex CollapseRegex();
}
