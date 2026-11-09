using System.Diagnostics.CodeAnalysis;

namespace api_faker_net.Requests;

public class MockRequestParser : IMockRequestParser
{
    public bool TryParseHeaders(
        IHeaderDictionary headers,
        [NotNullWhen(true)] out MockRequestOptions? options,
        [NotNullWhen(false)] out HeaderError? error)
    {
        options = null;

        int? delayMs = null;
        if (headers.TryGetValue(MockHeaders.Delay, out var delayValue))
        {
            if (!int.TryParse(delayValue, out var delay) || delay < 0)
            {
                error = new HeaderError(MockHeaders.Delay, delayValue.ToString(), "a non-negative integer (milliseconds)");
                return false;
            }
            delayMs = delay;
        }

        int? sequence = null;
        if (headers.TryGetValue(MockHeaders.Sequence, out var sequenceValue) &&
            !string.IsNullOrWhiteSpace(sequenceValue))
        {
            if (!int.TryParse(sequenceValue, out var parsedSequence))
            {
                error = new HeaderError(MockHeaders.Sequence, sequenceValue.ToString(), "an integer");
                return false;
            }
            sequence = parsedSequence;
        }

        int? statusCode = null;
        if (headers.TryGetValue(MockHeaders.SetHttpResponseCode, out var statusValue))
        {
            // 1xx can't be a final response, and Kestrel rejects anything outside 100–599.
            if (!int.TryParse(statusValue, out var status) || status is < 200 or > 599)
            {
                error = new HeaderError(MockHeaders.SetHttpResponseCode, statusValue.ToString(), "an HTTP status code between 200 and 599");
                return false;
            }
            statusCode = status;
        }

        error = null;
        options = new MockRequestOptions
        {
            // Presence is the signal; the value is ignored, so an empty "x-set-response:" still stores.
            IsStore = headers.ContainsKey(MockHeaders.SetResponse),
            IsDiagnostic = headers.ContainsKey(MockHeaders.Diagnostic),
            Sequence = sequence,
            StatusCode = statusCode,
            DelayMs = delayMs
        };
        return true;
    }
}
