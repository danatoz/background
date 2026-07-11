using Background.Dal.Entities;
using Background.Infrastructure.Pipeline;
using Background.Infrastructure.Pipeline.Steps;
using Background.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Background.Infrastructure.Tests.Steps;

public sealed class PreprocessingStepTests
{
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly ILogger<PreprocessingStep> _logger = Substitute.For<ILogger<PreprocessingStep>>();
    private readonly PreprocessingStep _step;

    public PreprocessingStepTests()
    {
        _step = new PreprocessingStep(_storage, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_StripsHtml_CollapsesWhitespace_AndSaves()
    {
        var context = new PipelineContext
        {
            ArtifactPrefix = "emails/2026/07/11/abc",
            RawContent = "<p>Hello  <b>world</b></p><p>How are you?</p>"
        };

        var result = await _step.ExecuteAsync(new ProcessingJob(), context, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Hello worldHow are you?", context.PreprocessedContent);

        await _storage.Received(1).SaveAsync(
            Arg.Is<string>(k => k.EndsWith("/preprocessed.md")),
            "Hello worldHow are you?",
            "text/markdown",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_HandlesPlainText_WithoutHtml()
    {
        var context = new PipelineContext
        {
            ArtifactPrefix = "emails/2026/07/11/abc",
            RawContent = "Just  a   plain text message"
        };

        var result = await _step.ExecuteAsync(new ProcessingJob(), context, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Just a plain text message", context.PreprocessedContent);
    }

    [Fact]
    public async Task ExecuteAsync_TrimsResult()
    {
        var context = new PipelineContext
        {
            ArtifactPrefix = "emails/2026/07/11/abc",
            RawContent = "   spaced out   "
        };

        var result = await _step.ExecuteAsync(new ProcessingJob(), context, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("spaced out", context.PreprocessedContent);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsEmpty_ForEmptyInput()
    {
        var context = new PipelineContext
        {
            ArtifactPrefix = "emails/2026/07/11/abc",
            RawContent = ""
        };

        var result = await _step.ExecuteAsync(new ProcessingJob(), context, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("", context.PreprocessedContent);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFail_WhenStorageThrows()
    {
        _storage
            .SaveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new IOException("disk full"));

        var context = new PipelineContext
        {
            ArtifactPrefix = "emails/2026/07/11/abc",
            RawContent = "some text"
        };

        var result = await _step.ExecuteAsync(new ProcessingJob(), context, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("disk full", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_StepName_IsCorrect()
    {
        Assert.Equal("Preprocessing", _step.StepName);
    }
}
