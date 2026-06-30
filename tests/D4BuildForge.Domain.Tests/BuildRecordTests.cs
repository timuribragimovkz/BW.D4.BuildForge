using System.Text.Json;
using D4BuildForge.Domain;

namespace D4BuildForge.Domain.Tests;

public class BuildRecordTests
{
    // Canonical §3.2 example — frozen build contract.
    private const string BuildJson = """
    {
      "id": "build_xyz",
      "name": "Druid Maul (live test #1)",
      "season": "s13",
      "class": "Druid",
      "level": 80,
      "skill": { "name": "Maul", "baseCoeff": 0.45, "ranks": 1 },
      "itemIds": [ "item_a", "item_b" ],
      "activeState": [ "Werebear" ],
      "target": { "level": 81, "armor": 0 },
      "expectedNonCrit": 510000
    }
    """;

    [Fact]
    public void Build_deserializes_including_class_keyword_and_nullable_expected()
    {
        var b = JsonSerializer.Deserialize<BuildRecord>(BuildJson, DomainJson.Options)!;
        Assert.Equal("build_xyz", b.Id);
        Assert.Equal("s13", b.Season);
        Assert.Equal("Druid", b.Class);           // from "class" key
        Assert.Equal(80, b.Level);
        Assert.Equal("Maul", b.Skill.Name);
        Assert.Equal(0.45, b.Skill.BaseCoeff);
        Assert.Equal(1, b.Skill.Ranks);
        Assert.Equal(2, b.ItemIds.Count);
        Assert.Single(b.ActiveState);
        Assert.Equal("Werebear", b.ActiveState[0]);
        Assert.Equal(81, b.Target.Level);
        Assert.Equal(510000, b.ExpectedNonCrit);
    }

    [Fact]
    public void Build_without_expected_leaves_it_null()
    {
        var json = BuildJson.Replace("\"expectedNonCrit\": 510000", "\"expectedNonCrit\": null");
        var b = JsonSerializer.Deserialize<BuildRecord>(json, DomainJson.Options)!;
        Assert.Null(b.ExpectedNonCrit);
    }

    [Fact]
    public void Build_roundtrips_with_lowercase_class_key()
    {
        var b = JsonSerializer.Deserialize<BuildRecord>(BuildJson, DomainJson.Options)!;
        var json = JsonSerializer.Serialize(b, DomainJson.Options);
        Assert.Contains("\"class\":\"Druid\"", json);
        Assert.DoesNotContain("\"Class\"", json);
    }
}
