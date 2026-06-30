using System.Text.Json.Nodes;
using D4BuildForge.Domain;

namespace D4BuildForge.Domain.Tests;

/// <summary>Test-only IRecordRepository fake; also a reference impl for Slice 2a's Api/Assembly tests.</summary>
public sealed class InMemoryRecordRepository : IRecordRepository
{
    private readonly Dictionary<string, Dictionary<string, JsonObject>> _store = new();

    public Task<IReadOnlyList<JsonObject>> List(string collection)
        => Task.FromResult<IReadOnlyList<JsonObject>>(
            _store.TryGetValue(collection, out var m) ? m.Values.Select(Clone).ToList() : []);

    public Task<JsonObject?> Get(string collection, string id)
        => Task.FromResult(
            _store.TryGetValue(collection, out var m) && m.TryGetValue(id, out var r) ? Clone(r) : null);

    public Task<JsonObject> Save(string collection, JsonObject record)
    {
        var id = record["id"]?.GetValue<string>();
        if (string.IsNullOrEmpty(id))
        {
            id = "rec_" + Guid.NewGuid().ToString("N");
            record["id"] = id;
        }
        if (!_store.TryGetValue(collection, out var m)) { m = new(); _store[collection] = m; }
        m[id] = Clone(record);
        return Task.FromResult(Clone(record));
    }

    public Task Delete(string collection, string id)
    {
        if (_store.TryGetValue(collection, out var m)) m.Remove(id);
        return Task.CompletedTask;
    }

    private static JsonObject Clone(JsonObject o) => (JsonObject)o.DeepClone();
}
