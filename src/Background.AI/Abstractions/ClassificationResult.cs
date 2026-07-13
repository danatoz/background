using System.Text.Json.Serialization;

namespace Background.AI.Abstractions;

public record ClassificationResult
{
    [JsonPropertyName("client_name")]
    public string ClientName { get; init; } = string.Empty;

    [JsonPropertyName("client_inn")]
    public string ClientInn { get; init; } = string.Empty;

    [JsonPropertyName("contract_number")]
    public string? ContractNumber { get; init; }

    [JsonPropertyName("contract_date")]
    public string? ContractDate { get; init; }

    [JsonPropertyName("document_type")]
    public string DocumentType { get; init; } = string.Empty;

    [JsonPropertyName("delivery_amount")]
    public DeliveryAmount? Amount { get; init; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; init; }
}

public record DeliveryAmount
{
    [JsonPropertyName("value")]
    public decimal Value { get; init; }

    [JsonPropertyName("currency")]
    public string Currency { get; init; } = string.Empty;
}
