using System.Reflection;
using System.Text.Json.Nodes;

namespace D4BuildForge.Import.Vessels;

/// Loads Mapping Vessels embedded in the assembly. v1 source; later this can front a
/// runtime config-vessel store so vessels are swappable without a rebuild.
public static class VesselStore
{
    static readonly Assembly Asm = typeof(VesselStore).Assembly;

    public static IReadOnlyList<JsonNode> All() =>
        Asm.GetManifestResourceNames()
           .Where(n => n.Contains(".Vessels.") && n.EndsWith(".json"))
           .Select(Read)
           .ToList();

    public static JsonNode Load(string source) =>
        All().FirstOrDefault(v => (string?)v["source"] == source)
        ?? throw new ImportException($"no vessel for source {source}");

    static JsonNode Read(string res)
    {
        using var s = Asm.GetManifestResourceStream(res)!;
        using var r = new StreamReader(s);
        return JsonNode.Parse(r.ReadToEnd())!;
    }
}
