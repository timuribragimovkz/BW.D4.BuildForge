using D4BuildForge.Import;

namespace D4BuildForge.Import.Tests;

public class BuildImporterTests
{
    static string Fixture(string f) => File.ReadAllText(Path.Combine("fixtures", f));

    [Fact] public void Detects_maxroll()
        => Assert.Equal(Contracts.BuildSource.Maxroll, new BuildImporter().FromJson(Fixture("maxroll_cataclysm.raw.json")).Source);

    [Fact] public void Detects_mobalytics()
        => Assert.Equal(Contracts.BuildSource.Mobalytics, new BuildImporter().FromJson(Fixture("mobalytics_zaior_landslide.lean.json")).Source);

    [Fact] public void Unknown_throws()
        => Assert.Throws<ImportException>(() => new BuildImporter().FromJson("{\"nothing\":true}"));
}
