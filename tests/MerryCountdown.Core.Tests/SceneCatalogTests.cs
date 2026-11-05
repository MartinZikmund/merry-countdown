using FluentAssertions;
using MerryCountdown.Core.Season;

namespace MerryCountdown.Core.Tests;

[TestClass]
public class SceneCatalogTests
{
    [TestMethod]
    public void Available_FreeVersion_OnlyFreeScenes()
    {
        SceneCatalog.Available(isFull: false).Should().OnlyContain(s => !s.RequiresFullVersion).And.HaveCount(2);
    }

    [TestMethod]
    public void Available_FullVersion_AllScenes()
    {
        SceneCatalog.Available(isFull: true).Should().HaveCount(SceneCatalog.All.Count);
    }

    [TestMethod]
    public void Resolve_PaidSceneWithoutFullVersion_FallsBackToFree()
    {
        var paid = SceneCatalog.All.First(s => s.RequiresFullVersion);

        SceneCatalog.Resolve(paid.Id, isFull: false).RequiresFullVersion.Should().BeFalse();
        SceneCatalog.Resolve(paid.Id, isFull: true).Id.Should().Be(paid.Id);
        SceneCatalog.Resolve("unknown", isFull: true).Should().Be(SceneCatalog.All[0]);
    }

    [TestMethod]
    public void Next_WrapsAroundOnlyOwnedScenes()
    {
        var free = SceneCatalog.Available(isFull: false);

        SceneCatalog.Next(free[0].Id, isFull: false).Should().Be(free[1]);
        SceneCatalog.Next(free[1].Id, isFull: false).Should().Be(free[0]);
    }
}
