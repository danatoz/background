using Amazon.S3;
using Background.Infrastructure.Pipeline;
using Background.Infrastructure.Pipeline.Steps;
using Background.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Background.Infrastructure;

public static class InfrastructureRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        services.AddOptions<S3Options>()
            .Configure<IConfiguration>((opts, config) => config.GetSection(S3Options.Section).Bind(opts));

        services.AddSingleton<IAmazonS3>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<S3Options>>().Value;
            var config = new AmazonS3Config
            {
                ServiceURL = opts.ServiceUrl,
                ForcePathStyle = opts.ForcePathStyle,
                UseHttp = true,
                Timeout = TimeSpan.FromSeconds(30)
            };
            return new AmazonS3Client(opts.AccessKey, opts.SecretKey, config);
        });

        services.AddSingleton<IStorageService, S3StorageService>();

        services.AddScoped<IProcessingStep, RawStorageStep>();
        services.AddScoped<IProcessingStep, PreprocessingStep>();
        services.AddScoped<IProcessingStep, LlmStep>();
        services.AddScoped<IProcessingStep, ValidationStep>();
        services.AddScoped<IProcessingStep, CompleteStep>();

        services.AddScoped<PipelineOrchestrator>();

        return services;
    }
}
