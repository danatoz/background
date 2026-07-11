using Background.Dal.Entities;
using Background.Dal.Repositories;
using Background.Infrastructure.Pipeline;
using Background.Infrastructure.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Background.Infrastructure.Tests.Pipeline;

public sealed class PipelineOrchestratorTests
{
    private readonly IProcessingJobRepository _repository = Substitute.For<IProcessingJobRepository>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly ILogger<PipelineOrchestrator> _logger = Substitute.For<ILogger<PipelineOrchestrator>>();

    private PipelineOrchestrator CreateOrchestrator(
        Action<IProcessingStep, int>? configureStep = null,
        PipelineOptions? options = null)
    {
        var steps = KnownSteps.All.Select((name, i) =>
        {
            var step = Substitute.For<IProcessingStep>();
            step.StepName.Returns(name);
            step.ExecuteAsync(
                    Arg.Any<ProcessingJob>(),
                    Arg.Any<PipelineContext>(),
                    Arg.Any<CancellationToken>())
                .Returns(ProcessingStepResult.Done);
            configureStep?.Invoke(step, i);
            return step;
        });

        return new PipelineOrchestrator(steps, _repository, _storage, _logger,
            Options.Create(options ?? new PipelineOptions()));
    }

    [Fact]
    public async Task RunAsync_RunsAllFourSteps_WhenStartingFresh()
    {
        var stepsRun = new List<string>();
        var orch = CreateOrchestrator((step, _) =>
        {
            step.ExecuteAsync(Arg.Any<ProcessingJob>(), Arg.Any<PipelineContext>(), Arg.Any<CancellationToken>())
                .Returns(ProcessingStepResult.Done)
                .AndDoes(_ => stepsRun.Add(step.StepName));
        });

        var message = new ProcessingJob { Id = Guid.NewGuid() };

        await orch.RunAsync(message, CancellationToken.None);

        Assert.Equal(KnownSteps.All, stepsRun);
        await _repository.Received(1).MarkCompletedAsync(message.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_SkipsCompletedSteps_WhenLastStepIsSet()
    {
        var stepsRun = new List<string>();
        var orch = CreateOrchestrator((step, _) =>
        {
            step.ExecuteAsync(Arg.Any<ProcessingJob>(), Arg.Any<PipelineContext>(), Arg.Any<CancellationToken>())
                .Returns(ProcessingStepResult.Done)
                .AndDoes(_ => stepsRun.Add(step.StepName));
        });

        var message = new ProcessingJob
        {
            Id = Guid.NewGuid(),
            LastStep = "Preprocessing"
        };

        await orch.RunAsync(message, CancellationToken.None);

        Assert.Equal(["Llm", "Validation", "Complete"], stepsRun);
        await _repository.Received(1).MarkCompletedAsync(message.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_StopsOnStepFailure_AndMarksFailed()
    {
        const string failAt = "Llm";
        var stepsRun = new List<string>();
        var orch = CreateOrchestrator((step, _) =>
        {
            step.ExecuteAsync(Arg.Any<ProcessingJob>(), Arg.Any<PipelineContext>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    stepsRun.Add(step.StepName);
                    return step.StepName == failAt
                        ? ProcessingStepResult.Fail("something went wrong")
                        : ProcessingStepResult.Done;
                });
        });

        var message = new ProcessingJob
        {
            Id = Guid.NewGuid(),
            RetryCount = 0
        };

        await orch.RunAsync(message, CancellationToken.None);

        Assert.Equal(["Preprocessing", "Llm"], stepsRun);
        await _repository.Received(1).MarkFailedAsync(
            message.Id,
            Arg.Is<string>(e => e.Contains("something went wrong")),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_StopsOnTerminalFailure_AndMarksFailed()
    {
        var stepsRun = new List<string>();
        var orch = CreateOrchestrator((step, _) =>
        {
            step.ExecuteAsync(Arg.Any<ProcessingJob>(), Arg.Any<PipelineContext>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    stepsRun.Add(step.StepName);
                    return step.StepName == "Validation"
                        ? ProcessingStepResult.TerminalFail("invalid data")
                        : ProcessingStepResult.Done;
                });
        });

        var message = new ProcessingJob { Id = Guid.NewGuid() };

        await orch.RunAsync(message, CancellationToken.None);

        Assert.Equal(["Preprocessing", "Llm", "Validation"], stepsRun);
        await _repository.Received(1).MarkFailedAsync(
            message.Id,
            Arg.Is<string>(e => e.Contains("Terminal failure")),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_NonTerminalFailure_UsesExponentialBackoff()
    {
        var orch = CreateOrchestrator((step, _) =>
        {
            step.ExecuteAsync(Arg.Any<ProcessingJob>(), Arg.Any<PipelineContext>(), Arg.Any<CancellationToken>())
                .Returns(ProcessingStepResult.Fail("error"));
        });

        var message = new ProcessingJob
        {
            Id = Guid.NewGuid(),
            RetryCount = 2
        };

        await orch.RunAsync(message, CancellationToken.None);

        await _repository.Received(1).MarkFailedAsync(
            message.Id,
            Arg.Any<string>(),
            Arg.Is<TimeSpan?>(d => d.HasValue && Math.Abs(d.Value.TotalSeconds - 40) < 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_TerminalFailure_DoesNotUseExponentialBackoff()
    {
        var orch = CreateOrchestrator((step, _) =>
        {
            step.ExecuteAsync(Arg.Any<ProcessingJob>(), Arg.Any<PipelineContext>(), Arg.Any<CancellationToken>())
                .Returns(ProcessingStepResult.TerminalFail("fatal"));
        });

        var message = new ProcessingJob { Id = Guid.NewGuid() };

        await orch.RunAsync(message, CancellationToken.None);

        await _repository.Received(1).MarkFailedAsync(
            message.Id,
            Arg.Any<string>(),
            Arg.Is<TimeSpan?>(d => !d.HasValue),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_StopsImmediately_WhenRetryCountExceedsMax()
    {
        var orch = CreateOrchestrator(options: new PipelineOptions { MaxRetries = 3 });

        var message = new ProcessingJob
        {
            Id = Guid.NewGuid(),
            RetryCount = 3,
            LastError = "previous error"
        };

        await orch.RunAsync(message, CancellationToken.None);

        await _repository.Received(1).MarkFailedAsync(
            message.Id,
            Arg.Is<string>(e => e.Contains("max retries")),
            Arg.Is<TimeSpan?>(d => !d.HasValue),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_ProceedsAsNormal_WhenRetryCountBelowMax()
    {
        var orch = CreateOrchestrator(options: new PipelineOptions { MaxRetries = 5 });

        var message = new ProcessingJob
        {
            Id = Guid.NewGuid(),
            RetryCount = 4
        };

        await orch.RunAsync(message, CancellationToken.None);

        await _repository.Received(1).MarkCompletedAsync(message.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenResumingAfterLlm_ReloadsResponseFromStorage()
    {
        var orch = CreateOrchestrator();
        var messageId = Guid.NewGuid();
        var prefix = "emails/2026/07/11/" + messageId.ToString("N");
        var message = new ProcessingJob
        {
            Id = messageId,
            LastStep = "Llm",
            ArtifactPrefix = prefix
        };

        _storage.GetAsync(Arg.Is<string>(k => k.EndsWith("/raw.json")), Arg.Any<CancellationToken>())
            .Returns("{}");
        _storage.GetAsync(Arg.Is<string>(k => k.EndsWith("/response.json")), Arg.Any<CancellationToken>())
            .Returns("""{"client_name":"Test"}""");

        await orch.RunAsync(message, CancellationToken.None);

        await _storage.Received(1).GetAsync(
            Arg.Is<string>(k => k.EndsWith("/response.json")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_SavesLastStepAfterEachStep()
    {
        var orch = CreateOrchestrator();
        var message = new ProcessingJob { Id = Guid.NewGuid() };

        await orch.RunAsync(message, CancellationToken.None);

        await _repository.Received(4).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_DoesNotSaveLastStep_WhenStepFails()
    {
        var orch = CreateOrchestrator((step, _) =>
        {
            step.ExecuteAsync(Arg.Any<ProcessingJob>(), Arg.Any<PipelineContext>(), Arg.Any<CancellationToken>())
                .Returns(ProcessingStepResult.Fail("fail"));
        });

        var message = new ProcessingJob { Id = Guid.NewGuid() };

        await orch.RunAsync(message, CancellationToken.None);

        await _repository.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_DoesNotReloadLlmResponse_WhenStartingFromBeginning()
    {
        var orch = CreateOrchestrator();
        var message = new ProcessingJob
        {
            Id = Guid.NewGuid(),
            ArtifactPrefix = "emails/2026/07/11/someprefix"
        };

        _storage.GetAsync(Arg.Is<string>(k => k.EndsWith("/raw.json")), Arg.Any<CancellationToken>())
            .Returns("{}");

        await orch.RunAsync(message, CancellationToken.None);

        await _storage.DidNotReceive().GetAsync(
            Arg.Is<string>(k => k.EndsWith("/response.json")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_DoesNotReloadLlmResponse_WhenPrefixIsEmpty()
    {
        var orch = CreateOrchestrator();
        var message = new ProcessingJob
        {
            Id = Guid.NewGuid(),
            LastStep = "Llm",
            ArtifactPrefix = null
        };

        await orch.RunAsync(message, CancellationToken.None);

        await _storage.DidNotReceive().GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_SetsContextArtifactPrefixAndRawContent_FromMessage()
    {
        PipelineContext? captured = null;
        var orch = CreateOrchestrator((step, _) =>
        {
            step.ExecuteAsync(Arg.Any<ProcessingJob>(), Arg.Do<PipelineContext>(ctx => captured = ctx), Arg.Any<CancellationToken>())
                .Returns(ProcessingStepResult.Done);
        });

        var prefix = "emails/2026/07/11/abc";
        var message = new ProcessingJob { Id = Guid.NewGuid(), ArtifactPrefix = prefix };

        _storage.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("""{"subject":"Hello"}""");

        await orch.RunAsync(message, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(prefix, captured!.ArtifactPrefix);
        Assert.Equal("""{"subject":"Hello"}""", captured.RawContent);
    }
}
