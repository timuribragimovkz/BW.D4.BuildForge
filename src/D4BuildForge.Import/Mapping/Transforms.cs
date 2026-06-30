using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace D4BuildForge.Import.Mapping;

/// The transform brick registry + pipeline runner. Each entry is an independent, agnostic
/// step; vessels reference them by name in `via` pipelines.
public static class Transforms
{
    public static readonly IReadOnlyDictionary<string, Func<TransformCtx, JsonNode?, string[], JsonNode?>> Registry =
        new Dictionary<string, Func<TransformCtx, JsonNode?, string[], JsonNode?>>
    {
        ["lower"]    = (_, n, _) => (string?)n is { } s ? (JsonNode?)s.ToLowerInvariant() : null,
        ["nullable"] = (_, n, _) => n,
        ["const"]    = (_, _, a) => ParseConst(a),
        ["format"]   = (_, n, a) => (JsonNode?)string.Format(a[0], (object?)(string?)n),
        ["bool"]     = (_, n, a) => (JsonNode?)(Truthy(n) ?? bool.Parse(a.Length > 0 ? a[0] : "false")),
        ["tokenAt"]  = (_, n, a) => { var p = ((string?)n)?.Split('_'); var i = int.Parse(a[0]); return p is not null && i < p.Length ? p[i] : null; },
        ["entries"]  = (_, n, _) => Entries(n),
        ["mergeDicts"] = (_, n, _) => MergeDicts(n),
        ["dropZero"] = (_, n, _) => Filter(n, IsNonZeroEntry),
        ["derefItemPool"] = (c, n, a) => DerefItemPool(c, n, a[0]),
        ["slotFromItemId"] = (c, n, a) => Lookup(Table(c, a[0]), ((string?)n)?.Split('_')[0]) ?? (JsonNode?)((string?)n)?.Split('_')[0],
        ["slotFromSlug"] = (c, n, a) => SlotFromSlug(Table(c, a[0]), (string?)n),
        ["countRepeats"] = (_, n, _) => CountRepeats(n),
        ["groupByRegex"] = (_, n, a) => GroupByRegex(n, a[0]),
        ["flattenBoons"] = (_, n, _) => FlattenBoons(n),
    };

    public static JsonNode? Run(TransformCtx ctx, JsonNode? input, IEnumerable<string> pipeline)
    {
        var node = input;
        foreach (var step in pipeline)
        {
            var (name, args) = Split(step);
            if (!Registry.TryGetValue(name, out var fn)) throw new ImportException($"unknown transform: {name}");
            node = fn(ctx, node, args);
        }
        return node;
    }

    static (string, string[]) Split(string step)
    {
        var i = step.IndexOf(':');
        return i < 0 ? (step, Array.Empty<string>()) : (step[..i], new[] { step[(i + 1)..] });
    }

    static JsonNode? ParseConst(string[] a)
    {
        if (a.Length == 0) return null;
        return a[0] switch
        {
            "null" => null,
            "true" => true,
            "false" => false,
            var s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) => d,
            var s => s
        };
    }

    static bool? Truthy(JsonNode? n) => n is null ? null :
        n is JsonValue v ? (v.TryGetValue(out bool b) ? b : v.TryGetValue(out double d) ? d != 0 : true) : true;

    static JsonArray Entries(JsonNode? n)
    {
        var a = new JsonArray();
        if (n is JsonObject o)
            foreach (var kv in o) a.Add(new JsonObject { ["@key"] = kv.Key, ["@value"] = kv.Value?.DeepClone() });
        return a;
    }

    static JsonObject MergeDicts(JsonNode? n)
    {
        var o = new JsonObject();
        if (n is JsonArray a)
            foreach (var d in a)
                if (d is JsonObject dd)
                    foreach (var kv in dd) o[kv.Key] = kv.Value?.DeepClone();
        return o;
    }

    static bool IsNonZeroEntry(JsonNode? e)
    {
        var v = (e as JsonObject)?["@value"];
        if (v is JsonValue jv && jv.GetValueKind() == JsonValueKind.Number) return (double)jv != 0;
        return true; // non-numeric values are kept
    }

    static JsonArray Filter(JsonNode? n, Func<JsonNode?, bool> keep)
    {
        var a = new JsonArray();
        if (n is JsonArray src) foreach (var e in src) if (keep(e)) a.Add(e?.DeepClone());
        return a;
    }

    static JsonObject? Table(TransformCtx c, string scopePath)
        => JsonPath.Select(scopePath.StartsWith("$vessel") ? c.Vessel : c.Root,
                           scopePath.Replace("$vessel", "$").Replace("$root", "$"))
                   .FirstOrDefault() as JsonObject;

    static JsonNode? Lookup(JsonObject? t, string? key)
        => key is not null && t?[key] is JsonNode v ? (JsonNode?)(string?)v : null;

    static JsonNode? SlotFromSlug(JsonObject? t, string? slug)
    {
        if (slug is null) return null;
        if (Lookup(t, slug) is JsonNode mapped) return mapped;
        if (slug.Contains("chaos-perk")) return "ChaosPerk";
        if (slug.EndsWith("seal") || slug.Contains("-seal")) return "Seal";
        if (slug.Contains("charm")) return "Charm";
        return slug;
    }

    static JsonNode? DerefItemPool(TransformCtx c, JsonNode? entries, string scopePath)
    {
        var pool = JsonPath.Select(c.Root, scopePath.Replace("$root", "$")).FirstOrDefault() as JsonObject;
        var a = new JsonArray();
        if (entries is JsonArray es && pool is not null)
            foreach (var e in es)
            {
                var key = (string?)(e as JsonObject)?["@key"];
                var poolKey = (e as JsonObject)?["@value"]?.ToString();
                if (poolKey is not null && pool[poolKey] is JsonObject item)
                {
                    var clone = (JsonObject)item.DeepClone();
                    clone["@key"] = key;
                    a.Add(clone);
                }
            }
        return a;
    }

    static JsonArray CountRepeats(JsonNode? n)
    {
        var order = new List<string>();
        var counts = new Dictionary<string, int>();
        if (n is JsonArray a)
            foreach (var e in a)
            {
                var k = (string?)e;
                if (k is null) continue;
                if (!counts.ContainsKey(k)) { counts[k] = 0; order.Add(k); }
                counts[k]++;
            }
        var outA = new JsonArray();
        foreach (var k in order) outA.Add(new JsonObject { ["@key"] = k, ["@count"] = counts[k] });
        return outA;
    }

    static JsonArray GroupByRegex(JsonNode? n, string pattern)
    {
        var rx = new Regex(pattern);
        var order = new List<string>();
        var groups = new Dictionary<string, JsonArray>();
        if (n is JsonArray a)
            foreach (var e in a)
            {
                var s = (string?)e;
                if (s is null) continue;
                var m = rx.Match(s);
                var g = m.Success ? m.Groups[1].Value : s;
                if (!groups.ContainsKey(g)) { groups[g] = new JsonArray(); order.Add(g); }
                groups[g].Add(s);
            }
        var outA = new JsonArray();
        foreach (var g in order) outA.Add(new JsonObject { ["@key"] = g, ["@members"] = groups[g] });
        return outA;
    }

    static JsonArray FlattenBoons(JsonNode? n)
    {
        var a = new JsonArray();
        if (n is JsonArray bs)
            foreach (var b in bs)
            {
                var animal = (string?)(b as JsonObject)?["type"];
                if ((b as JsonObject)?["vals"] is JsonArray vals)
                    foreach (var v in vals) a.Add(new JsonObject { ["@self"] = (string?)v, ["@animal"] = animal });
            }
        return a;
    }
}
