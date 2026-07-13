using System.Net.Http;
using Background.AI.Abstractions;
using Background.AI.Configuration;
using Background.AI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace Background.AI;

public static class AiRegistration
{
    public static IServiceCollection AddAi(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<LlmOptions>(configuration.GetSection(LlmOptions.Section));

        services.AddSingleton(sp =>
        {
            var opts = configuration.GetSection(LlmOptions.Section).Get<LlmOptions>() ?? new LlmOptions();
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds)
            };
            var builder = Kernel.CreateBuilder();

            if (!string.IsNullOrWhiteSpace(opts.Endpoint))
            {
                builder.AddOpenAIChatCompletion(
                    modelId: opts.ModelId,
                    endpoint: new Uri(opts.Endpoint),
                    apiKey: opts.ApiKey,
                    httpClient: httpClient);
            }
            else
            {
                builder.AddOpenAIChatCompletion(
                    modelId: opts.ModelId,
                    apiKey: opts.ApiKey,
                    httpClient: httpClient);
            }

            return builder.Build();
        });

        services.AddSingleton<LlmService>();
        services.AddSingleton<InvokePromptLlmService>();
        services.AddSingleton<ILlmService, StrategyLlmService>();

        return services;
    }
}
