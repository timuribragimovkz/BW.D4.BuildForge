namespace D4BuildForge.Import.Fetching;

/// Acquisition seam: fetch a source's raw build JSON from a URL. Keeps all network IO out of
/// the pure mapping core so the importer stays offline-testable.
public interface IBuildFetcher
{
    Task<string> FetchJsonAsync(string url, CancellationToken ct = default);
}
