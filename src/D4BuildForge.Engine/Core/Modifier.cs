using System.Collections.Generic;

namespace D4BuildForge.Engine.Core;

public record Modifier(
    StatChannel Channel,
    ModOp Op,
    double Value,
    BucketKey Bucket,
    IReadOnlySet<Tag> Conditions,
    SourceRef Source)
{
    public static Modifier Damage(BucketKey bucket, double value, SourceRef source, params Tag[] conditions)
        => new(StatChannel.Damage, ModOp.AdditivePercent, value, bucket, new HashSet<Tag>(conditions), source);

    public static Modifier Flat(StatChannel channel, double value, SourceRef source, params Tag[] conditions)
        => new(channel, ModOp.Flat, value, BucketKey.None, new HashSet<Tag>(conditions), source);
}
