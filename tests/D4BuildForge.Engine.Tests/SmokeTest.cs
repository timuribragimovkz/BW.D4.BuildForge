using Xunit;

namespace D4BuildForge.Engine.Tests;

public class SmokeTest
{
    [Fact]
    public void Approx_helper_passes_for_equal_values() => Approx.Equal(2543615.648, 2543615.648);
}
