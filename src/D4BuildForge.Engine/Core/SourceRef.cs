namespace D4BuildForge.Engine.Core;
public readonly record struct SourceRef(string Kind, string Name)
{
    public override string ToString() => $"{Kind}:{Name}";
}
