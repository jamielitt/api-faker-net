using System.Collections.Concurrent;
using api_faker_net.Models;

namespace api_faker_net.Storage;

/// <summary>
/// In-memory store of mock responses, keyed by "VERB:/path". Register as a singleton.
/// </summary>
public class RequestStorage : IRequestStorage
{
    private readonly ConcurrentDictionary<string, Route> _routes = new();

    public void Store(string verb, string path, StoredResponse entry)
    {
        var route = _routes.GetOrAdd(BuildKey(verb, path), _ => new Route());

        lock (route)
        {
            route.Responses.RemoveAll(r => r.Sequence == entry.Sequence);
            route.Responses.Add(entry);
            route.Responses.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));
        }
    }

    public StoredResponse? Get(string verb, string path, int? sequence = null)
    {
        if (!_routes.TryGetValue(BuildKey(verb, path), out var route))
        {
            return null;
        }

        lock (route)
        {
            if (route.Responses.Count == 0)
            {
                return null;
            }

            if (sequence is not null)
            {
                return route.Responses.Find(r => r.Sequence == sequence);
            }

            var response = route.Responses[route.NextIndex % route.Responses.Count];
            route.NextIndex = (route.NextIndex + 1) % route.Responses.Count;
            return response;
        }
    }

    private static string BuildKey(string verb, string path) => $"{verb.ToUpperInvariant()}:{path}";

    // A route's responses and its round-robin position, guarded by locking the Route itself.
    private sealed class Route
    {
        public List<StoredResponse> Responses { get; } = [];
        public int NextIndex { get; set; }
    }
}
