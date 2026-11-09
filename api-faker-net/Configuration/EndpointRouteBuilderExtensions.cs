using api_faker_net.Handlers;

namespace api_faker_net.Configuration;

public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the faker's catch-all endpoint: every verb and path goes to <see cref="IRequestHandler"/>.
    /// Map other endpoints (e.g. OpenAPI) first; this matches anything not matched elsewhere.
    /// </summary>
    public static IEndpointConventionBuilder MapApiFaker(this IEndpointRouteBuilder endpoints) =>
        endpoints.Map("/{**route}", (HttpContext context, IRequestHandler handler) => handler.HandleAsync(context));
}
