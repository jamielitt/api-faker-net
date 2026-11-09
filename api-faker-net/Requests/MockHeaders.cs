namespace api_faker_net.Requests;

/// <summary>
/// Names of the control headers this faker understands. They're meant for us, never the upstream API.
/// </summary>
public static class MockHeaders
{
    public const string SetResponse = "x-set-response";
    public const string Sequence = "x-sequence";
    public const string SetHttpResponseCode = "x-set-httpresponsecode";
    public const string Delay = "x-delay";
    public const string Diagnostic = "x-diagnostic";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        SetResponse, Sequence, SetHttpResponseCode, Delay, Diagnostic
    };
}
