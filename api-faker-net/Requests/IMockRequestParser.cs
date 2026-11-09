using System.Diagnostics.CodeAnalysis;

namespace api_faker_net.Requests;

public interface IMockRequestParser
{
    /// <summary>
    /// Reads the control headers. Returns false, with the first problem in <paramref name="error"/>,
    /// if any header has an invalid value.
    /// </summary>
    bool TryParseHeaders(
        IHeaderDictionary headers,
        [NotNullWhen(true)] out MockRequestOptions? options,
        [NotNullWhen(false)] out HeaderError? error);
}
