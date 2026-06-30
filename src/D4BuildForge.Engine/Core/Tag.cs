namespace D4BuildForge.Engine.Core;
public readonly record struct Tag(string Name)
{
    public static readonly Tag None = new("");
    public override string ToString() => Name;
}
