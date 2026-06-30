using Xunit;

namespace D4BuildForge.Engine.Tests;

internal static class Approx
{
    public static void Equal(double expected, double actual, double relTol = 1e-6)
    {
        var diff = System.Math.Abs(expected - actual);
        var tol = System.Math.Max(relTol * System.Math.Abs(expected), 1e-9);
        Assert.True(diff <= tol, $"Expected {expected}, got {actual} (diff {diff}, tol {tol})");
    }
}
