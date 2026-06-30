using System;
using D4BuildForge.Engine;
using D4BuildForge.Engine.Core;
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

    // ── LIVE GOLDEN FIXTURE #1 — Season 13, level 1 Druid, 1H mace + totem ──────────
    // Read off the live game (dummy, starting gear, no Paragon/passives):
    //   Willpower 10 · mace 16–24 (avg 20) + totem 16–24 (avg 20) = combined weapon 40
    //   Maul rank 1 = 80% (advanced tooltip) · no additive / multipliers / vulnerable
    // Cross-checks (both via Ava's model, ×0.2 intact):
    //   Maul tooltip number   = 0.80 × 40 × (1 + 10/800)        = 32.4 ≈ 32 (game showed 32)
    //   Actual per-hit (white) = tooltip × 0.2                   = 6.48
    //   Game dummy white hits  = 5,6,7,8 (avg 6.5)               → engine 6.48, ~0.3% match.
    // The off-hand totem's weapon damage SUMS into the main hand (combined = 40); the
    // famous "0.2" is the tooltip→actual-hit factor (validated, not an artifact).
    [Fact]
    public void Live_s13_lvl1_mace_plus_totem_matches_dummy()
        => AssertMatchesGame(
            new MaulScenario(
                Level: 1, MainStat: 10, WeaponDamage: 40,   // 40 = mace avg 20 + totem avg 20
                MaulBaseCoeff: 0.80, MaulRanks: 1,
                AdditivePct: 0, AllDamagePct: 0,
                CritChance: 0.05, CritDamagePct: 0,          // 50% sheet crit dmg = the BASE ×1.5, not a bucket
                AttackSpeedPct: 0, VulnerablePct: 0,
                TargetVulnerable: false, InWerebear: true,
                Season: Season.Current),
            expectedNonCrit: 6.5);                            // game dummy white-hit average

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
