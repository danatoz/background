using Background.AI.Abstractions;

namespace Background.AI.Services;

internal sealed class StrategyLlmService : ILlmService
{
    private readonly LlmService _chatCompletion;
    private readonly InvokePromptLlmService _invokePrompt;

    public StrategyLlmService(LlmService chatCompletion, InvokePromptLlmService invokePrompt)
    {
        _chatCompletion = chatCompletion;
        _invokePrompt = invokePrompt;
    }

    public async Task<LlmResult> ExecuteAsync(LlmRequest request, CancellationToken ct)
    {
        return request.Provider switch
        {
            "InvokePrompt" => await _invokePrompt.ExecuteAsync(request, ct),
            _ => await _chatCompletion.ExecuteAsync(request, ct)
        };
    }
}
