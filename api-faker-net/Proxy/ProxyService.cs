using api_faker_net.Configuration;
using api_faker_net.Requests;
using Microsoft.Extensions.Options;

namespace api_faker_net.Proxy;

/// <summary>
/// Forwards requests to the configured ProxyTarget. A typed HttpClient: register with
/// <see cref="ProxyServiceCollectionExtensions.AddProxyService"/>, which sets the base address.
/// </summary>
public class ProxyService(HttpClient httpClient) : IProxyService
{
    // Hop-by-hop and framing headers the HTTP stack sets itself, plus our own control headers.
    private static readonly HashSet<string> RestrictedRequestHeaders =
        new(["Host", "Content-Length", "Transfer-Encoding", "Connection", .. MockHeaders.All],
            StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> RestrictedResponseHeaders =
        new(["Transfer-Encoding", "Content-Length"], StringComparer.OrdinalIgnoreCase);

    public bool IsConfigured => httpClient.BaseAddress is not null;

    public async Task ForwardAsync(HttpContext context)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException("No ProxyTarget is configured; check IsConfigured first.");
        }

        var cancellationToken = context.RequestAborted;
        var request = context.Request;
        var targetUri = new Uri($"{request.Path}{request.QueryString}", UriKind.Relative);

        using var proxyRequest = new HttpRequestMessage(new HttpMethod(request.Method), targetUri);

        // Forward a body whenever the client sent one, whatever the verb (e.g. DELETE with a body).
        if (request.ContentLength > 0 || request.Headers.ContainsKey("Transfer-Encoding"))
        {
            using var ms = new MemoryStream();
            await request.Body.CopyToAsync(ms, cancellationToken);
            proxyRequest.Content = new ByteArrayContent(ms.ToArray());
        }

        foreach (var header in request.Headers)
        {
            if (RestrictedRequestHeaders.Contains(header.Key))
            {
                continue;
            }

            // Content headers (Content-Type, Content-Encoding, ...) can't go on the request itself,
            // so fall back to the content's headers when there is a body.
            if (!proxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                proxyRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        using var proxyResponse = await httpClient.SendAsync(proxyRequest, cancellationToken);
        var responseBytes = await proxyResponse.Content.ReadAsByteArrayAsync(cancellationToken);

        var response = context.Response;
        response.StatusCode = (int)proxyResponse.StatusCode;

        foreach (var header in proxyResponse.Headers.Concat(proxyResponse.Content.Headers))
        {
            if (RestrictedResponseHeaders.Contains(header.Key))
            {
                continue;
            }

            response.Headers[header.Key] = header.Value.ToArray();
        }

        // Responses like 204/304 (and HEAD) have no body, and Kestrel throws if we write one, even an empty one.
        if (responseBytes.Length > 0)
        {
            response.ContentType = proxyResponse.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            await response.Body.WriteAsync(responseBytes, cancellationToken);
        }
    }
}

public static class ProxyServiceCollectionExtensions
{
    public static IServiceCollection AddProxyService(this IServiceCollection services)
    {
        services.AddHttpClient<IProxyService, ProxyService>((sp, client) =>
            {
                var proxyTarget = sp.GetRequiredService<IOptions<AppConfiguration>>().Value.ProxyTarget;
                if (!string.IsNullOrWhiteSpace(proxyTarget))
                {
                    client.BaseAddress = new Uri(proxyTarget);
                }
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                // The default handler shares one cookie jar across every request, so a cookie the upstream
                // set for one caller would be sent on everyone's requests. Callers' own Cookie headers
                // are still forwarded as-is.
                UseCookies = false,

                // Pass redirects back to the caller rather than following them here.
                AllowAutoRedirect = false
            });

        return services;
    }
}
