using System.Text.Json;
using D4BuildForge.Import;
using D4BuildForge.Import.Contracts;

namespace D4BuildForge.Import.Tests;

public class ContractsTests
{
    [Fact]
    public void ExternalRef_serializes_camelCase_with_string_enum()
    {
        var r = new ExternalRef(BuildSource.Maxroll, "affix", "1829560");
        var json = JsonSerializer.Serialize(r, ImportJson.Options);
        Assert.Contains("\"source\": \"maxroll\"", json);
        Assert.Contains("\"kind\": \"affix\"", json);
        Assert.Contains("\"id\": \"1829560\"", json);
    }
}
