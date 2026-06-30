using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace D4BuildForge.Import.Mapping;

/// Minimal JSONPath subset evaluator. Grammar: leading `$`; `.key`; `[*]` (expand array);
/// `[n]` (index). `$` alone returns [current]. Missing keys yield nothing.
public static class JsonPath
{
    static readonly Regex Seg = new(@"^(?<key>[^.\[]+)?(?<idx>\[(?:\*|\d+)\])?$", RegexOptions.Compiled);

    public static IReadOnlyList<JsonNode?> Select(JsonNode? current, string path)
    {
        if (string.IsNullOrEmpty(path) || path == "$") return new[] { current };
        if (!path.StartsWith("$")) throw new ArgumentException($"path must start with $: {path}");

        IEnumerable<JsonNode?> cursor = new[] { current };
        foreach (var raw in path.Substring(1).TrimStart('.').Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var m = Seg.Match(raw);
            if (!m.Success) throw new ArgumentException($"bad path segment: {raw}");
            var key = m.Groups["key"].Success ? m.Groups["key"].Value : null;
            var idx = m.Groups["idx"].Success ? m.Groups["idx"].Value : null;

            var next = new List<JsonNode?>();
            foreach (var node in cursor)
            {
                var n = node;
                if (key is not null) n = (n as JsonObject)?[key];

                if (idx is null)
                {
                    if (n is not null) next.Add(n);
                }
                else if (n is JsonArray arr)
                {
                    if (idx == "[*]") next.AddRange(arr);
                    else
                    {
                        var i = int.Parse(idx.Trim('[', ']'));
                        if (i >= 0 && i < arr.Count) next.Add(arr[i]);
                    }
                }
            }
            cursor = next;
        }
        return cursor.ToList();
    }
}
