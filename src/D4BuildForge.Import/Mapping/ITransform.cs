using System.Text.Json.Nodes;

namespace D4BuildForge.Import.Mapping;

/// Context a transform can reach beyond its input: the build root (for $root.* lookups
/// like the Maxroll item pool) and the vessel document (for $vessel.* lookups like slot tables).
public record TransformCtx(JsonNode? Root, JsonNode Vessel);

/// Conceptual contract for a transform brick: an independent, agnostic step in a vessel's
/// `via` pipeline. The registry (Transforms.Registry) holds the concrete implementations.
public interface ITransform
{
    string Name { get; }
    JsonNode? Apply(TransformCtx ctx, JsonNode? input, string[] args);
}
