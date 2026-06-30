namespace D4BuildForge.Engine.Calc;

public record OffenseInputs(
    double WeaponDamage,
    double MainStatMult,
    double AdditiveNonCrit,
    double AdditiveCrit,
    double Vdm,
    double Csdm,
    double Admg,
    double SkillCoeff,
    double GlobalScalar,
    double BaseCritMult,
    double CritChance,
    double SealDmg,
    double SealCritDmg);
