using Background.Dal.Entities;
using Background.Infrastructure.Pipeline;
using Background.Infrastructure.Pipeline.Steps;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Background.Infrastructure.Tests.Steps;

public sealed class ValidationStepTests
{
    private readonly ILogger<ValidationStep> _logger = Substitute.For<ILogger<ValidationStep>>();
    private readonly ValidationStep _step;

    public ValidationStepTests()
    {
        _step = new ValidationStep(_logger);
    }

    private static string ValidJson => /*lang=json,strict*/ """
        {
            "client_name": "ООО Тест",
            "client_inn": "7701234567",
            "document_type": "Счёт-фактура",
            "delivery_amount": { "value": 1500.50, "currency": "RUB" },
            "confidence": 0.95
        }
        """;

    [Fact]
    public async Task ExecuteAsync_ReturnsSuccess_ForValidJson()
    {
        var context = new PipelineContext { LlmResponse = ValidJson };

        var result = await _step.ExecuteAsync(new ProcessingJob(), context, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ValidJson, context.ProcessedJson);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFail_WhenLlmResponseIsNull()
    {
        var context = new PipelineContext { LlmResponse = null };

        var result = await _step.ExecuteAsync(new ProcessingJob(), context, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("empty", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFail_WhenLlmResponseIsEmpty()
    {
        var context = new PipelineContext { LlmResponse = "" };

        var result = await _step.ExecuteAsync(new ProcessingJob(), context, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("empty", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFail_WhenJsonIsMalformed()
    {
        var context = new PipelineContext { LlmResponse = "not json" };

        var result = await _step.ExecuteAsync(new ProcessingJob(), context, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid JSON", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFail_WhenClientNameIsMissing()
    {
        var json = /*lang=json,strict*/ """{"client_inn":"7701234567","document_type":"x","delivery_amount":{"value":1,"currency":"RUB"},"confidence":0.5}""";
        var context = new PipelineContext { LlmResponse = json };

        var result = await _step.ExecuteAsync(new ProcessingJob(), context, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("client_name", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFail_WhenClientInnIsMissing()
    {
        var json = /*lang=json,strict*/ """{"client_name":"x","document_type":"x","delivery_amount":{"value":1,"currency":"RUB"},"confidence":0.5}""";
        var context = new PipelineContext { LlmResponse = json };

        var result = await _step.ExecuteAsync(new ProcessingJob(), context, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("client_inn", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFail_WhenDocumentTypeIsMissing()
    {
        var json = /*lang=json,strict*/ """{"client_name":"x","client_inn":"x","delivery_amount":{"value":1,"currency":"RUB"},"confidence":0.5}""";
        var context = new PipelineContext { LlmResponse = json };

        var result = await _step.ExecuteAsync(new ProcessingJob(), context, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("document_type", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFail_WhenDeliveryAmountIsNotObject()
    {
        var json = /*lang=json,strict*/ """{"client_name":"x","client_inn":"x","document_type":"x","delivery_amount":null,"confidence":0.5}""";
        var context = new PipelineContext { LlmResponse = json };

        var result = await _step.ExecuteAsync(new ProcessingJob(), context, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("delivery_amount", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFail_WhenDeliveryAmountValueIsNotNumber()
    {
        var json = /*lang=json,strict*/ """{"client_name":"x","client_inn":"x","document_type":"x","delivery_amount":{"value":"abc","currency":"RUB"},"confidence":0.5}""";
        var context = new PipelineContext { LlmResponse = json };

        var result = await _step.ExecuteAsync(new ProcessingJob(), context, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("delivery_amount.value", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsFail_WhenConfidenceIsNotNumber()
    {
        var json = /*lang=json,strict*/ """{"client_name":"x","client_inn":"x","document_type":"x","delivery_amount":{"value":1,"currency":"RUB"},"confidence":"high"}""";
        var context = new PipelineContext { LlmResponse = json };

        var result = await _step.ExecuteAsync(new ProcessingJob(), context, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("confidence", result.Error);
    }

    [Fact]
    public async Task ExecuteAsync_StepName_IsCorrect()
    {
        Assert.Equal("Validation", _step.StepName);
    }
}
