namespace api_faker_net.Handlers;

public interface IRequestTypeHandler
{
    public RequestType GetRequestType(HttpContext context);
}