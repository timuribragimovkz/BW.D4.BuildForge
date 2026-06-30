using D4BuildForge.Import.Vessels;

namespace D4BuildForge.Import.Tests.Vessels;

public class VesselStoreTests
{
    [Fact] public void Loads_maxroll_vessel() => Assert.Equal("maxroll", (string)VesselStore.Load("maxroll")["source"]!);
    [Fact] public void All_returns_both() => Assert.Equal(2, VesselStore.All().Count);
}
