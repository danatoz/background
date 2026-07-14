namespace Background.Infrastructure.Pipeline;

public interface IEmailBodyExtractor
{
    string ExtractBody(string rawJson);
}
