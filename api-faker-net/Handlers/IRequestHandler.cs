namespace api_faker_net.Handlers;

public interface IRequestHandler
{
    /// <summary>
    /// Handles any request to the faker: stores a response, describes the request (x-diagnostic),
    /// returns a stored response, or proxies to the ProxyTarget, in that order of precedence.
    /// </summary>
    Task<IResult> HandleAsync(HttpContext context);
}
