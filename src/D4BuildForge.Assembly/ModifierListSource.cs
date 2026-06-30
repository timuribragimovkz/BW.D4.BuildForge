using D4BuildForge.Engine.Core;

namespace D4BuildForge.Assembly;

/// <summary>Wraps a pre-built list of modifiers as an engine IModifierSource.</summary>
internal sealed class ModifierListSource(IReadOnlyList<Modifier> mods) : IModifierSource
{
    public IEnumerable<Modifier> GetModifiers() => mods;
}
