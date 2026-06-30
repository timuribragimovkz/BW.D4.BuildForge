namespace D4BuildForge.Engine.Calc;

public static class MainStatMultiplier
{
    public static double Compute(double mainStatSum, double divisor, double pooledMultPct)
        => 1 + mainStatSum * (1 + pooledMultPct) / divisor;
}
