using D4BuildForge.Domain;

namespace D4BuildForge.Domain.Tests;

public class DomainJsonTests
{
    [Fact]
    public void Options_are_camelCase_and_ignore_null_on_write()
    {
        Assert.Equal(System.Text.Json.JsonNamingPolicy.CamelCase, DomainJson.Options.PropertyNamingPolicy);
        Assert.True(DomainJson.Options.PropertyNameCaseInsensitive);
    }
}
