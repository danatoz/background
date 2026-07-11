namespace Background.AI.Abstractions;

public interface ILlmService
{
    Task<LlmResult> ExecuteAsync(LlmRequest request, CancellationToken ct = default);
}
