using System.Text.Json.Nodes;
using D4BuildForge.Import.Mapping;
using D4BuildForge.Import.Vessels;

namespace D4BuildForge.Import.Tests.Golden;

public class MobalyticsGoldenTests
{
    static JsonNode Fixture(string f) => JsonNode.Parse(File.ReadAllText(Path.Combine("fixtures", f)))!;

    [Fact]
    public void Imports_zaior_landslide_full_fidelity()
    {
        var build = new MappingEngine().Apply(VesselStore.Load("mobalytics"), Fixture("mobalytics_zaior_landslide.lean.json"));

        Assert.Equal(Contracts.BuildSource.Mobalytics, build.Source);
        Assert.Equal("druid", build.Clazz);
        Assert.Equal(3, build.Variants.Count);
        Assert.Equal(3, build.Warnings.Count);

        var end = build.Variants[2];
        Assert.Equal("End Game", end.Name);
        Assert.Null(end.Level);
        Assert.Null(end.WorldTier);
        Assert.Null(end.SkillBar);
        Assert.Equal(20, end.Gear.Count);
        Assert.Equal(27, end.SkillRanks.Count);
        Assert.Equal(5, end.Paragon.Boards.Count);
        Assert.Equal(5, end.ClassExtras.Count);

        var helm = end.Gear.Single(g => g.Slot == "Helm");
        Assert.Equal("gathlens-birthright", helm.Ref.Id);
        Assert.Null(helm.Explicits[0].Values);
        Assert.Contains(helm.Explicits, a => a.Greater);

        Assert.All(end.Paragon.Boards, b => Assert.Null(b.Glyph));
    }
}
