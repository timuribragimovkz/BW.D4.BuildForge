using System.Collections.Generic;

namespace D4BuildForge.Engine.Core;

public interface IModifierSource
{
    IEnumerable<Modifier> GetModifiers();
}
