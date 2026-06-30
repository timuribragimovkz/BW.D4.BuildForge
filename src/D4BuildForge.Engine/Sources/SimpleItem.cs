using System.Collections.Generic;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Sources;

public sealed class SimpleItem(string name, IReadOnlyList<Modifier> affixes) : IModifierSource
{
    public IEnumerable<Modifier> GetModifiers() => affixes;
}
