using System.Text.Json.Nodes;
using D4BuildForge.Domain;

namespace D4BuildForge.Domain.Tests;

public class RecordRepositoryContractTests
{
    [Fact]
    public async Task Save_assigns_id_when_absent_then_get_and_list_return_it()
    {
        IRecordRepository repo = new InMemoryRecordRepository();
        var saved = await repo.Save("items", new JsonObject { ["name"] = "Maul Helm" });

        var id = saved["id"]!.GetValue<string>();
        Assert.False(string.IsNullOrEmpty(id));

        var got = await repo.Get("items", id);
        Assert.Equal("Maul Helm", got!["name"]!.GetValue<string>());

        var all = await repo.List("items");
        Assert.Single(all);
    }

    [Fact]
    public async Task Save_keeps_existing_id_and_delete_removes()
    {
        IRecordRepository repo = new InMemoryRecordRepository();
        await repo.Save("builds", new JsonObject { ["id"] = "build_1", ["name"] = "A" });
        await repo.Delete("builds", "build_1");
        Assert.Null(await repo.Get("builds", "build_1"));
        Assert.Empty(await repo.List("builds"));
    }
}
