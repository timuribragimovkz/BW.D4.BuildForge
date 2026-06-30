using System.Collections.Generic;
using D4BuildForge.Engine.Calc;
using D4BuildForge.Engine.Core;
using D4BuildForge.Engine.Model;

namespace D4BuildForge.Engine.Pipeline;

public sealed class CalcContext(Build build, FormulaConfig config, ModifierPool pool, Breakdown breakdown)
{
    private readonly Dictionary<string, double> _values = new();
    public Build Build { get; } = build;
    public FormulaConfig Config { get; } = config;
    public ModifierPool Pool { get; } = pool;
    public Breakdown Breakdown { get; } = breakdown;

    public void Set(string key, double v) => _values[key] = v;
    public bool Has(string key) => _values.ContainsKey(key);
    public double Get(string key) => _values[key];
}
