namespace D4BuildForge.Engine.Core;
public readonly record struct BucketKey(string Name)
{
    public static readonly BucketKey None = new("");
    public static readonly BucketKey Additive = new("Additive");
    public static readonly BucketKey Vulnerable = new("Vulnerable");
    public static readonly BucketKey CritDamage = new("CritDamage");
    public static readonly BucketKey AllDamage = new("AllDamage");
    public override string ToString() => Name;
}
