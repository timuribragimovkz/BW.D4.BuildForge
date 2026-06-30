using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Config;

public interface ISeasonConfigProvider
{
    FormulaConfig Get(Season season);
}
