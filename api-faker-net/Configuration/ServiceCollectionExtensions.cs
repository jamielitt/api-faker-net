using api_faker_net.Handlers;
using api_faker_net.Proxy;
using api_faker_net.Requests;
using api_faker_net.Storage;

namespace api_faker_net.Configuration;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers everything the faker needs: configuration, storage, header parsing, the proxy and the request handler.
    /// </summary>
    public static IServiceCollection AddApiFaker(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppConfiguration>(configuration.GetSection(AppConfiguration.SectionName));
        services.AddSingleton<IRequestStorage, RequestStorage>();
        services.AddSingleton<IMockRequestParser, MockRequestParser>();
        services.AddProxyService();
        services.AddScoped<IRequestHandler, RequestHandler>();

        return services;
    }
}
