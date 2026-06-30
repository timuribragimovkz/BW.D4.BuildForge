using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using D4BuildForge.Import;
using D4BuildForge.Import.Fetching;

namespace D4BuildForge.Import.Tests.Fetching;

public class MaxrollFetcherTests
{
    sealed class StubHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage r, CancellationToken c)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "text/html") });
    }

    static async Task<HttpClient> StubFromFixture()
        => new HttpClient(new StubHandler(await File.ReadAllTextAsync(Path.Combine("fixtures", "maxroll_cataclysm_guide.html"))));

    [Fact]
    public async Task Extracts_plannerProfile_from_guide_html()
    {
        var json = await new MaxrollFetcher(await StubFromFixture())
            .FetchJsonAsync("https://maxroll.gg/d4/build-guides/cataclysm-druid-build-guide");
        var node = JsonNode.Parse(json)!;
        Assert.NotNull(node["data"]?["profiles"]);   // it's a plannerProfile
    }

    [Fact]
    public async Task FromMaxrollUrl_imports_end_to_end()
    {
        var build = await new BuildImporter().FromMaxrollUrlAsync(
            "https://maxroll.gg/d4/build-guides/cataclysm-druid-build-guide", new MaxrollFetcher(await StubFromFixture()));
        Assert.Equal(Contracts.BuildSource.Maxroll, build.Source);
        Assert.Equal("druid", build.Clazz);
        Assert.Equal(3, build.Variants.Count);
    }
}
