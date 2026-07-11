namespace Background.Api.Models;

public record CreateJobRequest(string Payload)
{
    public bool IsValid() => !string.IsNullOrWhiteSpace(Payload);
}