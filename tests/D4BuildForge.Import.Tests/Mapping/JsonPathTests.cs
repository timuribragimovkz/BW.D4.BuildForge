using System.Text.Json.Nodes;
using D4BuildForge.Import.Mapping;

namespace D4BuildForge.Import.Tests.Mapping;

public class JsonPathTests
{
    static JsonNode Node(string j) => JsonNode.Parse(j)!;

    [Fact] public void Self_returns_current()
        => Assert.Single(JsonPath.Select(Node("{\"a\":1}"), "$"));

    [Fact] public void Reads_nested_key()
    {
        var r = JsonPath.Select(Node("{\"a\":{\"b\":7}}"), "$.a.b");
        Assert.Single(r); Assert.Equal(7, (int)r[0]!);
    }

    [Fact] public void Wildcard_expands_array()
    {
        var r = JsonPath.Select(Node("{\"xs\":[1,2,3]}"), "$.xs[*]");
        Assert.Equal(3, r.Count);
    }

    [Fact] public void Wildcard_then_key_flattens()
    {
        var r = JsonPath.Select(Node("{\"s\":[{\"d\":1},{\"d\":2}]}"), "$.s[*].d");
        Assert.Equal(new[] { 1, 2 }, r.Select(n => (int)n!));
    }

    [Fact] public void Index_selects_one()
    {
        var r = JsonPath.Select(Node("{\"xs\":[10,20]}"), "$.xs[1]");
        Assert.Single(r); Assert.Equal(20, (int)r[0]!);
    }

    [Fact] public void Missing_key_yields_empty()
        => Assert.Empty(JsonPath.Select(Node("{\"a\":1}"), "$.nope"));
}
