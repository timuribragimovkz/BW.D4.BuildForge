using System.Collections.Generic;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Model;

public record Build(
    int Level,
    IReadOnlyList<IModifierSource> Sources,
    SkillSelection Skill,
    IReadOnlySet<Tag> ActiveState,
    Target Target);
