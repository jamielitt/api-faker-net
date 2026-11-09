namespace api_faker_net.Requests;

/// <summary>
/// A control header with a value that couldn't be parsed or is out of range.
/// </summary>
public record HeaderError(string Header, string Value, string Expected)
{
    public string Message => $"Invalid value for header '{Header}'. Expected {Expected}.";
}
