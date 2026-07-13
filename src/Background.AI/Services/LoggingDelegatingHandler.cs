using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Background.AI.Services;

internal sealed class LoggingDelegatingHandler : DelegatingHandler
{
    private readonly ILogger<LoggingDelegatingHandler> _logger;

    public LoggingDelegatingHandler(ILogger<LoggingDelegatingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery ?? "(null)";

        if (request.Content is not null)
        {
            var body = await request.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("LLM request: {Method} {Path}\n{Body}", request.Method, path, body);
            request.Content = new StringContent(body, Encoding.UTF8,
                request.Content.Headers.ContentType?.MediaType ?? "application/json");
        }
        else
        {
            _logger.LogInformation("LLM request: {Method} {Path} (no body)", request.Method, path);
        }

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var response = await base.SendAsync(request, cancellationToken);
        sw.Stop();

        if (response.Content is not null)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            _logger.LogInformation(
                "LLM response: {StatusCode} ({ElapsedMs}ms)\n{Body}",
                (int)response.StatusCode, sw.ElapsedMilliseconds, responseBody);

            response.Content = new StringContent(responseBody,
                Encoding.UTF8,
                response.Content.Headers.ContentType?.MediaType ?? "application/json");
        }
        else
        {
            _logger.LogInformation(
                "LLM response: {StatusCode} ({ElapsedMs}ms) (no body)",
                (int)response.StatusCode, sw.ElapsedMilliseconds);
        }

        return response;
    }
}
