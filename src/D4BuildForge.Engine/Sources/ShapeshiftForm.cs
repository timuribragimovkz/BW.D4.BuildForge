using System.Collections.Generic;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Sources;

public sealed class ShapeshiftForm(Tag form, IReadOnlyList<Modifier> formBonuses) : IModifierSource
{
    public Tag Form { get; } = form;
    public IEnumerable<Modifier> GetModifiers() => formBonuses;
}
