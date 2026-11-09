namespace api_faker_net.Handlers;

public class RequestTypeHandler : IRequestTypeHandler
{
    private const string SetResponse = "x-set-response";
    
    public RequestType GetRequestType(HttpContext context)
    {
        // The header's presence is the signal; its value is ignored (an empty "x-set-response:" still stores).
        return context.Request.Headers.ContainsKey(SetResponse) ?
                RequestType.WriteToStore : RequestType.ReadFromStore;
    }
}