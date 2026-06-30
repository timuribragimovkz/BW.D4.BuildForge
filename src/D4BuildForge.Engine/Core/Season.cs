namespace D4BuildForge.Engine.Core;

public readonly record struct Season(string Id)
{
    public static readonly Season Current = new("s13");
    public override string ToString() => Id;
}
