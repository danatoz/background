using Background.Dal.Entities;

namespace Background.Infrastructure.Pipeline;

public interface IProcessingStep
{
    string StepName { get; }
    Task<IProcessingStepResult> ExecuteAsync(ProcessingJob message, PipelineContext context, CancellationToken ct);
}

public interface IProcessingStepResult
{
    bool IsSuccess { get; }
    string? Error { get; }
    bool IsTerminal { get; }
}

public sealed record ProcessingStepResult(bool IsSuccess, string? Error = null, bool IsTerminal = false) : IProcessingStepResult
{
    public static readonly ProcessingStepResult Done = new(true);
    public static ProcessingStepResult Fail(string error) => new(false, error);
    public static ProcessingStepResult TerminalFail(string error) => new(false, error, IsTerminal: true);
}
