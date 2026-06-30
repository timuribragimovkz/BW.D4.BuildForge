using D4BuildForge.Engine.Core;

namespace D4BuildForge.Engine.Tests.Core;

public class VocabularyTests
{
    [Fact]
    public void Tags_with_same_name_are_equal()
        => Assert.Equal(new Tag("Werebear"), new Tag("Werebear"));

    [Fact]
    public void BucketKey_constants_have_expected_names()
    {
        Assert.Equal("Additive", BucketKey.Additive.Name);
        Assert.Equal("Vulnerable", BucketKey.Vulnerable.Name);
    }
}
