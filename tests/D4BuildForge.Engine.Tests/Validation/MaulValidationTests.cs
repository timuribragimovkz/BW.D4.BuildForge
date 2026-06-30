using System;
using D4BuildForge.Engine;
using D4BuildForge.Engine.Model;

namespace D4BuildForge.Engine.Tests.Validation;

public class MaulValidationTests
{
    // === GOLDEN FIXTURES: one row per verified-against-game Druid Maul scenario. ===
    // Seed row uses representative numbers (NonCrit 23.4). Replace/add rows with REAL
    // in-game values + the tooltip number you read; a passing row is a locked baseline.

    [Fact]
    public void Seed_scenario_matches_expected()
        => AssertMatchesGame(MaulScenario.Seed(), expectedNonCrit: 23.4);

    private static void AssertMatchesGame(MaulScenario scenario, double expectedNonCrit)
    {
        CalcResult result = BuildCalculator.Calculate(scenario.ToBuild(), scenario.Season);
        double diff = Math.Abs(expectedNonCrit - result.NonCrit);
        double tol = Math.Max(1e-2 * Math.Abs(expectedNonCrit), 1e-9); // ≤1% per spec
        Assert.True(diff <= tol,
            $"Engine NonCrit {result.NonCrit} != expected {expectedNonCrit} (diff {diff} > tol {tol}).\n" +
            $"Breakdown:\n{BreakdownFormatter.Format(result.Breakdown)}");
    }
}
