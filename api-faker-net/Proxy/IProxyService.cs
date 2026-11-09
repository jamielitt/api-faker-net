namespace api_faker_net.Proxy;

public interface IProxyService
{
    /// <summary>True when a ProxyTarget is configured.</summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Sends the incoming request to the ProxyTarget and writes the upstream's response
    /// (status, headers and body) to <see cref="HttpContext.Response"/>.
    /// </summary>
    Task ForwardAsync(HttpContext context);
}
