using System.Text.Json;
using D4BuildForge.Domain;

namespace D4BuildForge.Domain.Tests;

public class ItemRecordTests
{
    // Canonical §3.1 example — this JSON IS the frozen item contract.
    private const string ItemJson = """
    {
      "id": "item_abc",
      "sourceId": "Pants_Legendary_Generic_053",
      "name": "Tibault's Will",
      "slot": "Pants",
      "itemType": "Pants",
      "rarity": "Unique",
      "itemPower": 800,
      "classRestriction": null,
      "affixes": [
        { "kind": "explicit", "stat": "MainStat",   "value": 120, "isGreater": false },
        { "kind": "implicit", "stat": "Vulnerable", "value": 0.15 },
        { "kind": "tempered", "stat": "MaulRanks",  "value": 2 }
      ],
      "aspect": null,
      "sockets": [ "Gem_Sapphire_04", null ]
    }
    """;

    [Fact]
    public void Item_deserializes_with_all_fields()
    {
        var item = JsonSerializer.Deserialize<ItemRecord>(ItemJson, DomainJson.Options)!;
        Assert.Equal("item_abc", item.Id);
        Assert.Equal("Pants_Legendary_Generic_053", item.SourceId);
        Assert.Equal("Tibault's Will", item.Name);
        Assert.Equal("Pants", item.Slot);
        Assert.Equal("Unique", item.Rarity);
        Assert.Equal(800, item.ItemPower);
        Assert.Null(item.ClassRestriction);
        Assert.Equal(3, item.Affixes.Count);
        Assert.Equal("explicit", item.Affixes[0].Kind);
        Assert.Equal("MainStat", item.Affixes[0].Stat);
        Assert.Equal(120, item.Affixes[0].Value);
        Assert.Equal("implicit", item.Affixes[1].Kind);
        Assert.False(item.Affixes[1].IsGreater); // absent in JSON -> default false
        Assert.Null(item.Aspect);
        Assert.Equal(2, item.Sockets.Count);
        Assert.Equal("Gem_Sapphire_04", item.Sockets[0]);
        Assert.Null(item.Sockets[1]); // empty socket
    }

    [Fact]
    public void Item_roundtrips_to_camelCase_json()
    {
        var item = JsonSerializer.Deserialize<ItemRecord>(ItemJson, DomainJson.Options)!;
        var json = JsonSerializer.Serialize(item, DomainJson.Options);
        Assert.Contains("\"itemPower\":800", json);
        Assert.Contains("\"kind\":\"explicit\"", json);
        Assert.DoesNotContain("\"ItemPower\"", json); // not PascalCase
    }
}
