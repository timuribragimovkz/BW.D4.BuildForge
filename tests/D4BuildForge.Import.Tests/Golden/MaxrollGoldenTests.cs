using System.Text.Json.Nodes;
using D4BuildForge.Import.Mapping;
using D4BuildForge.Import.Vessels;

namespace D4BuildForge.Import.Tests.Golden;

public class MaxrollGoldenTests
{
    static JsonNode Fixture(string f) => JsonNode.Parse(File.ReadAllText(Path.Combine("fixtures", f)))!;

    [Fact]
    public void Imports_cataclysm_full_fidelity()
    {
        var build = new MappingEngine().Apply(VesselStore.Load("maxroll"), Fixture("maxroll_cataclysm.raw.json"));

        Assert.Equal(Contracts.BuildSource.Maxroll, build.Source);
        Assert.Equal("druid", build.Clazz);
        Assert.Equal(3, build.Variants.Count);

        var end = build.Variants[2];
        Assert.Equal("Endgame", end.Name);
        Assert.Equal(70, end.Level);
        Assert.Equal(14, end.WorldTier);
        Assert.Equal(16, end.Gear.Count);
        Assert.Equal(35, end.SkillRanks.Count);
        Assert.Equal(25, end.Paragon.Boards.Count);
        Assert.Equal(5, end.ClassExtras.Count);
        Assert.NotNull(end.SkillBar);

        var helm = end.Gear.Single(g => g.Slot == "Helm");
        Assert.Equal("Helm_Unique_Druid_102", helm.Ref.Id);
        Assert.True(helm.Mythic);
        Assert.NotNull(helm.Explicits[0].Values);

        var board0 = end.Paragon.Boards[0];
        Assert.NotNull(board0.Glyph);
        Assert.NotNull(board0.GlyphLevel);
    }
}
