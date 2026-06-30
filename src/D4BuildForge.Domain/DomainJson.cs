using System.Text.Json;
using System.Text.Json.Serialization;

namespace D4BuildForge.Domain;

/// <summary>Canonical (de)serialization options for every Domain record. camelCase JSON.</summary>
public static class DomainJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };
}
