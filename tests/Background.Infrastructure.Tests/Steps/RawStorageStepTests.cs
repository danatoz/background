using Background.Dal.Entities;
using Background.Infrastructure.Pipeline;
using Background.Infrastructure.Pipeline.Steps;
using Background.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Background.Infrastructure.Tests.Steps;

public sealed class RawStorageStepTests
{
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly ILogger<RawStorageStep> _logger = Substitute.For<ILogger<RawStorageStep>>();
    private readonly RawStorageStep _step;

    public RawStorageStepTests()
    {
        _step = new RawStorageStep(_storage, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_SavesRawContentToStorage()
    {
        var message = new InboxMessage { Id = Guid.NewGuid() };
        var context = new PipelineContext
        {
            ArtifactPrefix = "emails/2026/07/11/abc",
            RawContent = """{"subject":"Hello"}"""
        };

        var result = await _step.ExecuteAsync(message, context, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _storage.Received(1).SaveAsync(
            Arg.Is<string>(k => k.EndsWith("/raw.json")),
            context.RawContent,
            "application/json",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFail_WhenStorageThrows()
    {
        _storage
            .SaveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("connection failed"));

        var context = new PipelineContext
        {
            ArtifactPrefix = "emails/2026/07/11/abc",
            RawContent = "{}"
        };

        var result = await _step.ExecuteAsync(new InboxMessage(), context, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("connection failed", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_StepName_IsCorrect()
    {
        Assert.Equal("RawStorage", _step.StepName);
    }
}
