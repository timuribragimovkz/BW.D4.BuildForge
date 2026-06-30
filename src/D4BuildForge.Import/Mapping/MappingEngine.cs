using System.Text.Json;
using System.Text.Json.Nodes;
using D4BuildForge.Import.Contracts;

namespace D4BuildForge.Import.Mapping;

/// Interprets a Mapping Vessel (data) against a parsed source document to produce an ImportedBuild.
/// Binding forms:
///   "$.path" / "$root.path" / "$vessel.path" — JSONPath read against a scope.
///   "@token"  — context token (@self/@key/@value/@count/@members/@animal) read off current.
///   "@name"   — reference to another named binding block in the vessel.
///   { from, via?, each? } — read a path, pipe through transforms, optionally map each element.
///   { via:["const", <literal>] } — emit a JSON literal.
///   { "$ref": { kind, id, name? } } — build an ExternalRef.
///   { ...fields } — inline object, each field evaluated.
public sealed class MappingEngine
{
    static readonly HashSet<string> ContextTokens = new()
        { "@self", "@key", "@value", "@count", "@members", "@animal" };

    public ImportedBuild Apply(JsonNode vessel, JsonNode raw)
    {
        var rootPath = (string?)vessel["root"] ?? "$";
        var root = JsonPath.Select(raw, rootPath).FirstOrDefault()
                   ?? throw new ImportException($"vessel root path matched nothing: {rootPath}");
        var ctx = new TransformCtx(root, vessel);

        var built = (JsonObject)(Eval(vessel["build"]!, root, root, ctx)
                                 ?? throw new ImportException("build binding produced nothing"));
        built["schemaVersion"] ??= "1.0";
        built["source"] ??= (string?)vessel["source"];
        built["capturedAtUtc"] ??= JsonValue.Create(DateTime.UtcNow);
        built["warnings"] ??= new JsonArray();

        return built.Deserialize<ImportedBuild>(ImportJson.Options)
               ?? throw new ImportException("failed to materialize ImportedBuild");
    }

    // current = node that $. paths read from; root = build root (for $root.* + transform ctx).
    JsonNode? Eval(JsonNode binding, JsonNode? current, JsonNode? root, TransformCtx ctx)
    {
        switch (binding)
        {
            case JsonValue v when v.TryGetValue(out string? s) && s is not null:
                return EvalString(s, current, root, ctx);
            case JsonObject o when o.ContainsKey("$ref"):
                return EvalRef((JsonObject)o["$ref"]!, current, root, ctx);
            case JsonObject o when o.ContainsKey("from") || o.ContainsKey("via"):
                return EvalFromVia(o, current, root, ctx);
            case JsonObject o:
                var res = new JsonObject();
                foreach (var kv in o) res[kv.Key] = Eval(kv.Value!, current, root, ctx);
                return res;
            default:
                return binding.DeepClone();
        }
    }

    JsonNode? EvalString(string s, JsonNode? current, JsonNode? root, TransformCtx ctx)
    {
        if (s.StartsWith("@"))
        {
            if (ContextTokens.Contains(s))
                return s == "@self" ? current?.DeepClone() : (current as JsonObject)?[s]?.DeepClone();
            // block reference
            var block = ctx.Vessel[s.Substring(1)] ?? throw new ImportException($"no vessel block: {s}");
            return Eval(block, current, root, ctx);
        }
        if (!s.StartsWith("$")) return s;                                   // literal string
        if (s.StartsWith("$root")) return First(JsonPath.Select(root, s.Replace("$root", "$")));
        if (s.StartsWith("$vessel")) return First(JsonPath.Select(ctx.Vessel, s.Replace("$vessel", "$")));
        return First(JsonPath.Select(current, s));
    }

    JsonNode? EvalFromVia(JsonObject o, JsonNode? current, JsonNode? root, TransformCtx ctx)
    {
        var viaArr = o["via"] as JsonArray;

        // const-literal form: {"via":["const", <literal>]} — emit literal (2nd element may be non-string).
        if (viaArr is { Count: >= 1 } && (string?)viaArr[0] == "const")
            return viaArr.Count > 1 ? viaArr[1]?.DeepClone() : null;

        var via = viaArr?.Select(x => (string)x!).ToList();

        JsonNode? src = o.ContainsKey("from")
            ? CollectFrom((string)o["from"]!, current, root, ctx)
            : current?.DeepClone();
        if (via is not null) src = Transforms.Run(ctx, src, via);

        if (o["each"] is JsonNode each)
        {
            var outA = new JsonArray();
            if (src is JsonArray arr)
                foreach (var el in arr) outA.Add(Eval(each, el, root, ctx));
            return outA;
        }
        return src;
    }

    // "from" yields multiple nodes (wildcards) -> array; a "@token" -> read off current; else single node.
    JsonNode? CollectFrom(string from, JsonNode? current, JsonNode? root, TransformCtx ctx)
    {
        if (from.StartsWith("@"))
            return from == "@self" ? current?.DeepClone() : (current as JsonObject)?[from]?.DeepClone();

        var scope = from.StartsWith("$root") ? root : from.StartsWith("$vessel") ? ctx.Vessel : current;
        var path = from.Replace("$root", "$").Replace("$vessel", "$");
        var matches = JsonPath.Select(scope, path);
        if (path.Contains("[*]"))
        {
            var a = new JsonArray();
            foreach (var m in matches) a.Add(m?.DeepClone());
            return a;
        }
        return First(matches);
    }

    JsonNode EvalRef(JsonObject r, JsonNode? current, JsonNode? root, TransformCtx ctx)
    {
        var refObj = new JsonObject { ["source"] = (string?)ctx.Vessel["source"] };
        refObj["kind"] = Eval(r["kind"]!, current, root, ctx);
        refObj["id"] = Eval(r["id"]!, current, root, ctx)?.ToString();
        if (r.ContainsKey("name")) refObj["name"] = Eval(r["name"]!, current, root, ctx);
        return refObj;
    }

    static JsonNode? First(IReadOnlyList<JsonNode?> ns) => ns.Count > 0 ? ns[0]?.DeepClone() : null;
}
