namespace Background.Api.Models;

public record CreateMessageRequest(string Payload)
{
    public bool IsValid() => !string.IsNullOrWhiteSpace(Payload);
}