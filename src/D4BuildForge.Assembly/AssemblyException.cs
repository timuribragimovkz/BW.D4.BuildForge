namespace D4BuildForge.Assembly;

/// <summary>Thrown when a BuildRecord/ItemRecord cannot be mapped to engine input (e.g. an unknown affix stat).</summary>
public sealed class AssemblyException(string message) : Exception(message);
