using Background.Dal.Entities;
using Background.Infrastructure.Pipeline;
using Background.Infrastructure.Pipeline.Steps;
using Background.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Background.Infrastructure.Tests.Steps;

public sealed class CompleteStepTests
{
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly ILogger<CompleteStep> _logger = Substitute.For<ILogger<CompleteStep>>();
    private readonly CompleteStep _step;

    public CompleteStepTests()
    {
        _step = new CompleteStep(_storage, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_SavesProcessedJson_WhenPresent()
    {
        var context = new PipelineContext
        {
            ArtifactPrefix = "emails/2026/07/11/abc",
            ProcessedJson = """{"client_name":"Test"}"""
        };

        var result = await _step.ExecuteAsync(new ProcessingJob(), context, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _storage.Received(1).SaveAsync(
            Arg.Is<string>(k => k.EndsWith("/processed.json")),
            context.ProcessedJson,
            "application/json",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SkipsSave_WhenProcessedJsonIsNull()
    {
        var context = new PipelineContext
        {
            ArtifactPrefix = "emails/2026/07/11/abc",
            ProcessedJson = null
        };

        var result = await _step.ExecuteAsync(new ProcessingJob(), context, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _storage.DidNotReceive().SaveAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_SavesEmptyString_WhenProcessedJsonIsEmpty()
    {
        var context = new PipelineContext
        {
            ArtifactPrefix = "emails/2026/07/11/abc",
            ProcessedJson = ""
        };

        var result = await _step.ExecuteAsync(new ProcessingJob(), context, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _storage.Received(1).SaveAsync(
            Arg.Is<string>(k => k.EndsWith("/processed.json")),
            "",
            "application/json",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFail_WhenStorageThrows()
    {
        _storage
            .SaveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new IOException("upload failed"));

        var context = new PipelineContext
        {
            ArtifactPrefix = "emails/2026/07/11/abc",
            ProcessedJson = "{}"
        };

        var result = await _step.ExecuteAsync(new ProcessingJob(), context, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("upload failed", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_StepName_IsCorrect()
    {
        Assert.Equal("Complete", _step.StepName);
    }
}
