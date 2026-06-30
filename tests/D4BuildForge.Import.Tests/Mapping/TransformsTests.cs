using System.Text.Json.Nodes;
using D4BuildForge.Import.Mapping;

namespace D4BuildForge.Import.Tests.Mapping;

public class TransformsTests
{
    static TransformCtx Ctx(string root = "{}") => new(JsonNode.Parse(root), new JsonObject { ["source"] = "maxroll" });
    static JsonNode N(string j) => JsonNode.Parse(j)!;

    [Fact] public void Lower() => Assert.Equal("druid", (string)Transforms.Run(Ctx(), N("\"Druid\""), new[] { "lower" })!);

    [Fact] public void Entries_emits_key_value()
    {
        var r = Transforms.Run(Ctx(), N("{\"10\":1,\"31\":0}"), new[] { "entries" })!.AsArray();
        Assert.Equal(2, r.Count);
        Assert.Equal("10", (string)r[0]!["@key"]!); Assert.Equal(1, (int)r[0]!["@value"]!);
    }

    [Fact] public void DropZero_removes_zero_ranks()
    {
        var r = Transforms.Run(Ctx(), N("{\"10\":1,\"31\":0}"), new[] { "entries", "dropZero" })!.AsArray();
        Assert.Single(r);
    }

    [Fact] public void CountRepeats_counts_first_seen()
    {
        var r = Transforms.Run(Ctx(), N("[\"a\",\"a\",\"b\"]"), new[] { "countRepeats" })!.AsArray();
        Assert.Equal("a", (string)r[0]!["@key"]!); Assert.Equal(2, (int)r[0]!["@count"]!);
        Assert.Equal("b", (string)r[1]!["@key"]!); Assert.Equal(1, (int)r[1]!["@count"]!);
    }

    [Fact] public void GroupByRegex_groups_by_board_prefix()
    {
        var r = Transforms.Run(Ctx(), N("[\"d-a-x1-y2\",\"d-a-x3-y4\",\"d-b-x1-y1\"]"),
                               new[] { @"groupByRegex:^(.*)-x\d+-y\d+$" })!.AsArray();
        Assert.Equal(2, r.Count);
        Assert.Equal("d-a", (string)r[0]!["@key"]!);
        Assert.Equal(2, r[0]!["@members"]!.AsArray().Count);
    }

    [Fact] public void SlotFromItemId_uses_prefix_table()
    {
        var ctx = new TransformCtx(null, new JsonObject { ["slotTable"] = new JsonObject { ["Helm"] = "Helm" } });
        var r = Transforms.Run(ctx, N("\"Helm_Unique_Druid_102\""), new[] { "slotFromItemId:$vessel.slotTable" });
        Assert.Equal("Helm", (string)r!);
    }

    [Fact] public void DerefItemPool_pulls_item_and_keeps_key()
    {
        var root = N("{\"items\":{\"3\":{\"id\":\"Pants_X\"}}}");
        var ctx = new TransformCtx(root, new JsonObject());
        var entries = Transforms.Run(ctx, N("{\"5\":3}"), new[] { "entries" }); // slot 5 -> pool key 3
        var r = Transforms.Run(ctx, entries, new[] { "derefItemPool:$root.items" })!.AsArray();
        Assert.Single(r);
        Assert.Equal("Pants_X", (string)r[0]!["id"]!);
        Assert.Equal("5", (string)r[0]!["@key"]!);
    }
}
