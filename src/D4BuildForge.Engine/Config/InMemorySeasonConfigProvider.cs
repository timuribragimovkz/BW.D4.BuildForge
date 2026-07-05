using System.Collections.Generic;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Config;

public sealed class InMemorySeasonConfigProvider : ISeasonConfigProvider
{
    private readonly IReadOnlyDictionary<string, FormulaConfig> _bySeason;

    public InMemorySeasonConfigProvider()
        : this(new Dictionary<string, FormulaConfig>
        {
            ["s13"] = FormulaConfig.Druid,
            ["s14"] = FormulaConfig.Druid, // clone of s13 — S14 left the Druid damage formula unchanged
        })
    { }

    public InMemorySeasonConfigProvider(IReadOnlyDictionary<string, FormulaConfig> bySeason)
        => _bySeason = bySeason;

    public FormulaConfig Get(Season season)
        => _bySeason.TryGetValue(season.Id, out var cfg)
            ? cfg
            : throw new KeyNotFoundException($"No config for season '{season.Id}'.");
}
