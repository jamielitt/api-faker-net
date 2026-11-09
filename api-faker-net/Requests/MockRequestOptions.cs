namespace api_faker_net.Requests;

/// <summary>
/// The control headers of a request, parsed and validated. A null value means the header wasn't sent.
/// </summary>
public record MockRequestOptions
{
    /// <summary>x-set-response was sent (with any value, including empty): store the body as a response.</summary>
    public bool IsStore { get; init; }

    /// <summary>x-diagnostic was sent: describe the request instead of answering it.</summary>
    public bool IsDiagnostic { get; init; }

    /// <summary>x-sequence: which response to store or return. An empty header counts as not sent.</summary>
    public int? Sequence { get; init; }

    /// <summary>x-set-httpresponsecode: the status code to store with the response (200–599).</summary>
    public int? StatusCode { get; init; }

    /// <summary>x-delay: milliseconds to wait before returning the stored response.</summary>
    public int? DelayMs { get; init; }
}
