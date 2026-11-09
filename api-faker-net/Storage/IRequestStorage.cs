using api_faker_net.Models;

namespace api_faker_net.Storage;

public interface IRequestStorage
{
    /// <summary>
    /// Stores a response for the route. A response with the same sequence number replaces the existing one.
    /// </summary>
    void Store(string verb, string path, StoredResponse entry);

    /// <summary>
    /// Gets a stored response for the route, or null if there isn't one.
    /// With a sequence number, returns that exact response; without one, cycles through
    /// the route's responses in sequence order (round-robin).
    /// </summary>
    StoredResponse? Get(string verb, string path, int? sequence = null);
}
