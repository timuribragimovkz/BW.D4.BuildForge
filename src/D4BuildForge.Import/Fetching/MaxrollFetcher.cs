namespace D4BuildForge.Import.Fetching;

/// Fetches a Maxroll build-guide (or planner) URL and extracts the embedded plannerProfile JSON.
/// Maxroll is server-rendered (Remix) and returns HTTP 200 to a plain GET, so no browser is needed.
public sealed class MaxrollFetcher(HttpClient http) : IBuildFetcher
{
    public async Task<string> FetchJsonAsync(string url, CancellationToken ct = default)
    {
        var html = await http.GetStringAsync(url, ct);
        return ExtractPlannerProfile(html);
    }

    /// Locate `"plannerProfile":` and return the brace-balanced JSON object that follows
    /// (string-aware, so braces inside string values don't confuse the matcher).
    public static string ExtractPlannerProfile(string html)
    {
        const string key = "\"plannerProfile\"";
        var i = html.IndexOf(key, StringComparison.Ordinal);
        if (i < 0) throw new ImportException("plannerProfile not found in page");

        var start = html.IndexOf('{', html.IndexOf(':', i));
        if (start < 0) throw new ImportException("plannerProfile value not found");

        int depth = 0;
        bool instr = false, esc = false;
        for (var j = start; j < html.Length; j++)
        {
            var c = html[j];
            if (esc) { esc = false; continue; }
            if (c == '\\') { esc = true; continue; }
            if (c == '"') { instr = !instr; continue; }
            if (instr) continue;
            if (c == '{') depth++;
            else if (c == '}' && --depth == 0) return html.Substring(start, j - start + 1);
        }
        throw new ImportException("unterminated plannerProfile object");
    }
}
