namespace api_faker_net.Models;

public record StoredResponse
{
    public int Sequence { get; init; }
    public int StatusCode { get; init; } = 200;
    public int DelayMs { get; init; }
    public string Body { get; init; } = string.Empty;
    public string ContentType { get; init; } = "application/json";
}