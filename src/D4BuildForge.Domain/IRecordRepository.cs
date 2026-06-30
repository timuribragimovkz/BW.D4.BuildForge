using System.Text.Json.Nodes;

namespace D4BuildForge.Domain;

/// <summary>
/// Persistence contract for game-content / build *collections*. Works in raw JsonObject so the
/// schemaless render engine round-trips records losslessly. Real impl: DynamoRecordRepository
/// (src/Storage, Slice 2a) over AWS DynamoDB d4bf_* tables.
/// </summary>
public interface IRecordRepository
{
    Task<IReadOnlyList<JsonObject>> List(string collection);
    Task<JsonObject?> Get(string collection, string id);

    /// <summary>Upsert. Assigns a fresh <c>id</c> when the record has none; returns the stored record.</summary>
    Task<JsonObject> Save(string collection, JsonObject record);

    Task Delete(string collection, string id);
}
