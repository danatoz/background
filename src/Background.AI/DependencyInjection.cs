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
            var builder = Kernel.CreateBuilder();

            if (!string.IsNullOrWhiteSpace(opts.Endpoint))
            {
                builder.AddOpenAIChatCompletion(
                    modelId: opts.ModelId,
                    endpoint: new Uri(opts.Endpoint),
                    apiKey: opts.ApiKey);
            }
            else
            {
                builder.AddOpenAIChatCompletion(
                    modelId: opts.ModelId,
                    apiKey: opts.ApiKey);
            }

            return builder.Build();
        });

        services.AddSingleton<ILlmService, LlmService>();

        return services;
    }
}
