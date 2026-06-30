using System.Collections.Generic;
using D4BuildForge.Engine.Calc;
using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Pipeline.Stages;

public sealed class OffenseBucketsStage : IStage
{
    public IReadOnlySet<string> Reads { get; } = new HashSet<string> { Keys.MainStatSum, Keys.CritChance };
    public IReadOnlySet<string> Writes { get; } = new HashSet<string> { Keys.NonCrit, Keys.Crit, Keys.Average };

    public void Run(CalcContext ctx)
    {
        var pool = ctx.Pool;
        var cfg = ctx.Config;

        double weapon = pool.FlatSum(StatChannel.WeaponDamage);
        double pooledMainStat = pool.BucketSum(new BucketKey("MainStatMult"));
        double mainStatMult = MainStatMultiplier.Compute(ctx.Get(Keys.MainStatSum), cfg.MainStatDivisor, pooledMainStat);

        double additive = BucketResolver.BucketValue(pool, cfg, BucketKey.Additive);
        double vdm = BucketResolver.BucketValue(pool, cfg, BucketKey.Vulnerable);
        double csdm = BucketResolver.BucketValue(pool, cfg, BucketKey.CritDamage);
        double admg = BucketResolver.BucketValue(pool, cfg, BucketKey.AllDamage);

        int totalRanks = ctx.Build.Skill.Ranks + (int)pool.FlatSum(StatChannel.SkillRank);
        double skillCoeff = SkillCoefficient.Compute(ctx.Build.Skill.BaseCoeff, totalRanks);

        var inputs = new OffenseInputs(
            WeaponDamage: weapon, MainStatMult: mainStatMult,
            AdditiveNonCrit: additive, AdditiveCrit: additive,
            Vdm: vdm, Csdm: csdm, Admg: admg,
            SkillCoeff: skillCoeff, GlobalScalar: cfg.GlobalSkillScalar, BaseCritMult: cfg.BaseCritMultiplier,
            CritChance: ctx.Get(Keys.CritChance),
            SealDmg: pool.BucketSum(new BucketKey("SealDmg")),
            SealCritDmg: pool.BucketSum(new BucketKey("SealCritDmg")));

        var r = OffenseCalculator.Compute(inputs);
        ctx.Set(Keys.NonCrit, r.NonCrit);
        ctx.Set(Keys.Crit, r.Crit);
        ctx.Set(Keys.Average, r.Average);

        ctx.Breakdown.Add("WeaponDamage", weapon);
        ctx.Breakdown.Add("MainStatMult", mainStatMult);
        ctx.Breakdown.Add("Additive", additive, "1 + Σ additive %");
        ctx.Breakdown.Add("VDM", vdm);
        ctx.Breakdown.Add("CSDM", csdm);
        ctx.Breakdown.Add("ADMG", admg);
        ctx.Breakdown.Add("SkillCoeff", skillCoeff);
        ctx.Breakdown.Add(Keys.NonCrit, r.NonCrit);
        ctx.Breakdown.Add(Keys.Crit, r.Crit);
        ctx.Breakdown.Add(Keys.Average, r.Average);
    }
}
