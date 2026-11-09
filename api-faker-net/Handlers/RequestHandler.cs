using System.Text;
using api_faker_net.Models;
using api_faker_net.Proxy;
using api_faker_net.Requests;
using api_faker_net.Storage;

namespace api_faker_net.Handlers;

public class RequestHandler(
    IMockRequestParser parser,
    IRequestStorage storage,
    IProxyService proxy) : IRequestHandler
{
    public async Task<IResult> HandleAsync(HttpContext context)
    {
        if (!parser.TryParseHeaders(context.Request.Headers, out var options, out var headerError))
        {
            return Results.Json(new
            {
                error = headerError.Message,
                header = headerError.Header,
                value = headerError.Value
            }, statusCode: StatusCodes.Status400BadRequest);
        }

        var httpVerb = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";

        // 1. Storing a response
        if (options.IsStore)
        {
            return await StoreAsync(context, httpVerb, path, options);
        }

        // 2. Explicit diagnostic request always wins from here on
        if (options.IsDiagnostic)
        {
            return Diagnostic(httpVerb, path, options);
        }

        // 3. Returning a previously stored response
        var stored = storage.Get(httpVerb, path, options.Sequence);
        if (stored is not null)
        {
            return await ReplayAsync(stored, context.RequestAborted);
        }

        // 4. Nothing stored for this route -> proxy to the configured upstream
        if (!proxy.IsConfigured)
        {
            return Results.Json(new
            {
                error = "No stored response found and no ProxyTarget is configured.",
                httpVerb,
                path
            }, statusCode: StatusCodes.Status502BadGateway);
        }

        await proxy.ForwardAsync(context);
        return Results.Empty;
    }

    private async Task<IResult> StoreAsync(HttpContext context, string httpVerb, string path, MockRequestOptions options)
    {
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
        var body = await reader.ReadToEndAsync(context.RequestAborted);

        var entry = new StoredResponse
        {
            Sequence = options.Sequence ?? 0,
            StatusCode = options.StatusCode ?? StatusCodes.Status200OK,
            DelayMs = options.DelayMs ?? 0,
            Body = body,
            ContentType = context.Request.ContentType ?? "application/json"
        };

        storage.Store(httpVerb, path, entry);

        return Results.Json(new
        {
            stored = true,
            httpVerb,
            path,
            sequence = entry.Sequence,
            statusCode = entry.StatusCode,
            delayMs = entry.DelayMs,
            bodyLength = body.Length
        });
    }

    private static IResult Diagnostic(string httpVerb, string path, MockRequestOptions options) =>
        Results.Json(new
        {
            httpVerb,
            path,
            response = "x-set-response header not set. This will be used to load the cache for this endpoint with data",
            delay = options.DelayMs?.ToString() ?? "No value set",
            sequence = options.Sequence?.ToString() ?? "This will set the order of how the responses are returned from the cache with this endpoint",
            httpResponseCode = options.StatusCode?.ToString() ?? "x-set-httpresponsecode header not set",
        }, statusCode: options.StatusCode ?? StatusCodes.Status404NotFound);

    private static async Task<IResult> ReplayAsync(StoredResponse stored, CancellationToken cancellationToken)
    {
        if (stored.DelayMs > 0)
        {
            await Task.Delay(stored.DelayMs, cancellationToken);
        }

        // 204 and 304 responses must not have a body; Kestrel throws if we try to write one.
        if (stored.StatusCode is StatusCodes.Status204NoContent or StatusCodes.Status304NotModified)
        {
            return Results.StatusCode(stored.StatusCode);
        }

        return Results.Content(stored.Body, stored.ContentType, statusCode: stored.StatusCode);
    }
}
