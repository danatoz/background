using Background.AI.Abstractions;
using Background.AI.Configuration;
using Background.Dal.Entities;
using Background.Infrastructure.Pipeline;
using Background.Infrastructure.Pipeline.Steps;
using Background.Infrastructure.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Background.Infrastructure.Tests.Steps;

public sealed class LlmStepTests
{
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly IActivePromptCache _promptCache = Substitute.For<IActivePromptCache>();
    private readonly ILlmService _llmService = Substitute.For<ILlmService>();
    private readonly IOptions<LlmOptions> _options;
    private readonly ILogger<LlmStep> _logger = Substitute.For<ILogger<LlmStep>>();
    private readonly LlmStep _step;

    public LlmStepTests()
    {
        _options = Options.Create(new LlmOptions
        {
            PromptName = "inbox-classification",
            UseStructuredOutput = true
        });
        _step = new LlmStep(_storage, _promptCache, _llmService, _options, _logger);
    }

    [Fact]
    public async Task ExecuteAsync_CallsLlmAndSavesArtifacts()
    {
        var prompt = new Prompt
        {
            Id = Guid.NewGuid(),
            Content = "Classify this: {{content}}",
            SystemPrompt = "You are a classifier",
            ModelName = "gpt-4o-mini",
            Temperature = 0.1
        };

        _promptCache.GetActiveAsync("inbox-classification", Arg.Any<CancellationToken>())
            .Returns(prompt);

        _llmService.ExecuteAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(new LlmResult
            {
                Content = """{"client_name":"Test"}""",
                ModelUsed = "gpt-4o-mini",
                PromptTokens = 100,
                CompletionTokens = 20,
                FinishReason = LlmFinishReason.Stop,
                Duration = TimeSpan.FromMilliseconds(500)
            });

        var message = new InboxMessage { Id = Guid.NewGuid() };
        var context = new PipelineContext
        {
            ArtifactPrefix = "emails/2026/07/11/abc",
            PreprocessedContent = "Some invoice data"
        };

        var result = await _step.ExecuteAsync(message, context, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(prompt.Id, message.PromptId);
        Assert.Equal("Classify this: Some invoice data", context.Prompt);
        Assert.Equal("""{"client_name":"Test"}""", context.LlmResponse);

        await _storage.Received(1).SaveAsync(
            Arg.Is<string>(k => k.EndsWith("/prompt.md")),
            Arg.Any<string>(),
            "text/markdown",
            Arg.Any<CancellationToken>());

        await _storage.Received(1).SaveAsync(
            Arg.Is<string>(k => k.EndsWith("/response.json")),
            Arg.Any<string>(),
            "application/json",
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFail_WhenPromptNotFound()
    {
        _promptCache.GetActiveAsync("inbox-classification", Arg.Any<CancellationToken>())
            .Returns((Prompt?)null);

        var result = await _step.ExecuteAsync(
            new InboxMessage { Id = Guid.NewGuid() },
            new PipelineContext { ArtifactPrefix = "em/abc" },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFail_WhenLlmReturnsEmpty()
    {
        var prompt = new Prompt
        {
            Id = Guid.NewGuid(),
            Content = "Classify: {{content}}"
        };

        _promptCache.GetActiveAsync("inbox-classification", Arg.Any<CancellationToken>())
            .Returns(prompt);

        _llmService.ExecuteAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(new LlmResult
            {
                Content = "",
                FinishReason = LlmFinishReason.ContentFilter
            });

        var context = new PipelineContext
        {
            ArtifactPrefix = "emails/2026/07/11/abc",
            PreprocessedContent = "data"
        };

        var result = await _step.ExecuteAsync(
            new InboxMessage { Id = Guid.NewGuid() },
            context,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("empty", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFail_WhenLlmThrows()
    {
        var prompt = new Prompt
        {
            Id = Guid.NewGuid(),
            Content = "Classify: {{content}}"
        };

        _promptCache.GetActiveAsync("inbox-classification", Arg.Any<CancellationToken>())
            .Returns(prompt);

        _llmService.ExecuteAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<LlmResult>(new HttpRequestException("timeout")));

        var context = new PipelineContext
        {
            ArtifactPrefix = "emails/2026/07/11/abc",
            PreprocessedContent = "data"
        };

        var result = await _step.ExecuteAsync(
            new InboxMessage { Id = Guid.NewGuid() },
            context,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("timeout", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_FallsBackToRawContent_WhenNoPreprocessedContent()
    {
        var prompt = new Prompt
        {
            Id = Guid.NewGuid(),
            Content = "Process: {{content}}"
        };

        _promptCache.GetActiveAsync("inbox-classification", Arg.Any<CancellationToken>())
            .Returns(prompt);

        _llmService.ExecuteAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(new LlmResult { Content = "ok" });

        var context = new PipelineContext
        {
            ArtifactPrefix = "emails/2026/07/11/abc",
            RawContent = "raw data",
            PreprocessedContent = null
        };

        var result = await _step.ExecuteAsync(
            new InboxMessage { Id = Guid.NewGuid() },
            context,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Process: raw data", context.Prompt);
    }

    [Fact]
    public async Task ExecuteAsync_UsesStructuredOutput_WhenConfigured()
    {
        var prompt = new Prompt
        {
            Id = Guid.NewGuid(),
            Content = "Classify: {{content}}"
        };

        _promptCache.GetActiveAsync("inbox-classification", Arg.Any<CancellationToken>())
            .Returns(prompt);

        LlmRequest? captured = null;
        _llmService.ExecuteAsync(Arg.Any<LlmRequest>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                captured = callInfo.Arg<LlmRequest>();
                return new LlmResult { Content = "ok" };
            });

        var context = new PipelineContext
        {
            ArtifactPrefix = "emails/2026/07/11/abc",
            PreprocessedContent = "data"
        };

        await _step.ExecuteAsync(new InboxMessage { Id = Guid.NewGuid() }, context, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(LlmResponseFormat.JsonObject, captured!.ResponseFormat);
    }

    [Fact]
    public async Task ExecuteAsync_StepName_IsCorrect()
    {
        Assert.Equal("Llm", _step.StepName);
    }
}
