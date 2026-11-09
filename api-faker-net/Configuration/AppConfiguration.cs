namespace api_faker_net.Configuration;

public class AppConfiguration
{
    public const string SectionName = "AppConfiguration";

    /// <summary>
    /// Base URL to proxy requests to when no stored response exists for a route.
    /// e.g. "https://example.com"
    /// </summary>
    public string? ProxyTarget { get; init; }
}
