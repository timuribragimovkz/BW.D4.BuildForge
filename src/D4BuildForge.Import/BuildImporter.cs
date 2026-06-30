using System.Text.Json.Nodes;
using D4BuildForge.Import.Contracts;
using D4BuildForge.Import.Fetching;
using D4BuildForge.Import.Mapping;
using D4BuildForge.Import.Vessels;

namespace D4BuildForge.Import;

/// Entry point: turns a raw source payload into an ImportedBuild. Auto-detects the source by
/// matching each vessel's `match.anyPathExists` against the raw document.
public sealed class BuildImporter
{
    readonly MappingEngine _engine = new();

    public ImportedBuild FromJson(string json)
    {
        var raw = JsonNode.Parse(json) ?? throw new ImportException("invalid json");
        var vessel = PickVessel(raw) ?? throw new ImportException("no matching source vessel");
        return _engine.Apply(vessel, raw);
    }

    public async Task<ImportedBuild> FromMaxrollUrlAsync(string url, IBuildFetcher fetcher, CancellationToken ct = default)
        => FromJson(await fetcher.FetchJsonAsync(url, ct));

    public JsonNode? PickVessel(JsonNode raw) =>
        VesselStore.All().FirstOrDefault(v =>
            (v["match"]?["anyPathExists"] as JsonArray)?.Any(p =>
                JsonPath.Select(raw, (string)p!).Any(n => n is not null)) == true);
}
