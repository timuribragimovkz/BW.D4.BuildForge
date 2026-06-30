using System.Text.Json.Nodes;
using D4BuildForge.Import.Mapping;

namespace D4BuildForge.Import.Tests.Mapping;

public class MappingEngineTests
{
    static JsonNode N(string j) => JsonNode.Parse(j)!;

    [Fact]
    public void Maps_name_class_and_each_variant_with_ref_and_skillranks()
    {
        var vessel = N(@"{
          ""source"":""maxroll"", ""root"":""$.data"",
          ""build"":{ ""clazz"":{""from"":""$.class"",""via"":[""lower""]}, ""name"":""$.name"",
                      ""variants"":{""from"":""$.profiles"",""each"":""@variant""} },
          ""variant"":{ ""name"":""$.name"",
                        ""skillRanks"":{""from"":""$.skillTree.steps[*].data"",""via"":[""mergeDicts"",""entries"",""dropZero""],
                                        ""each"":{""ref"":{""$ref"":{""kind"":""skill"",""id"":""@key""}},""rank"":""@value""}} }
        }");
        var raw = N(@"{""data"":{""class"":""Druid"",""name"":""B"",
            ""profiles"":[{""name"":""End"",""skillTree"":{""steps"":[{""data"":{""416"":15,""413"":0}}]}}]}}");

        var build = new MappingEngine().Apply(vessel, raw);

        Assert.Equal("druid", build.Clazz);
        Assert.Equal("B", build.Name);
        var v = Assert.Single(build.Variants);
        Assert.Equal("End", v.Name);
        var sr = Assert.Single(v.SkillRanks);
        Assert.Equal("416", sr.Ref.Id);
        Assert.Equal(15, sr.Rank);
        Assert.Equal(Contracts.BuildSource.Maxroll, sr.Ref.Source);
    }

    [Fact]
    public void Const_literal_array_and_block_reference_from_token()
    {
        // exercises: const-literal array (warnings), @block ref, @members as a from-source
        var vessel = N(@"{
          ""source"":""mobalytics"", ""root"":""$"",
          ""build"":{ ""warnings"":{""via"":[""const"",[""w1"",""w2""]]},
                      ""variants"":{""from"":""$.vs"",""each"":""@variant""} },
          ""variant"":{ ""name"":""$.name"", ""level"":{""via"":[""const:null""]} }
        }");
        var raw = N(@"{""vs"":[{""name"":""A""}]}");

        var build = new MappingEngine().Apply(vessel, raw);
        Assert.Equal(new[] { "w1", "w2" }, build.Warnings);
        Assert.Equal(Contracts.BuildSource.Mobalytics, build.Source);
        Assert.Null(build.Variants[0].Level);
    }
}
