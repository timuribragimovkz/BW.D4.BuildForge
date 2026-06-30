namespace D4BuildForge.Engine.Calc;

public static class SkillCoefficient
{
    public static double Compute(double baseCoeff, int totalRanks)
    {
        int fifths = totalRanks / 5;
        return baseCoeff * (1 + 0.10 * (totalRanks - fifths - 1) + 0.15 * fifths);
    }
}
