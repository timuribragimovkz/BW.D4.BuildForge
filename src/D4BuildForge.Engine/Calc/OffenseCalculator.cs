namespace D4BuildForge.Engine.Calc;

public static class OffenseCalculator
{
    public static OffenseResult Compute(OffenseInputs i)
    {
        double common = i.WeaponDamage * i.MainStatMult * i.Vdm * i.Admg * i.SkillCoeff * i.GlobalScalar;
        double nonCrit = common * i.AdditiveNonCrit * (1 + i.SealDmg);
        double crit = common * i.AdditiveCrit * i.Csdm * i.BaseCritMult * (1 + i.SealCritDmg + i.SealDmg);
        double avg = crit * i.CritChance + nonCrit * (1 - i.CritChance);
        return new OffenseResult(nonCrit, crit, avg);
    }
}
