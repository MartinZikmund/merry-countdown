using FluentAssertions;
using MerryCountdown.Core.Family;

namespace MerryCountdown.Core.Tests;

[TestClass]
public class JsonFamilyStoreTests
{
    private string _directory = null!;

    [TestInitialize]
    public void Setup() => _directory = Directory.CreateTempSubdirectory("merry").FullName;

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_directory, recursive: true);

    [TestMethod]
    public void RoundTrip_KeepsKidsWishesAndDoors()
    {
        JsonFamilyStore store = new(_directory);
        FamilyData data = new() { Kids = [new Kid { Name = "Ema", Wishes = [new Wish { Text = "Žluťoučký kůň", Sticker = "toy" }] }] };
        data.Kids[0].OpenedDoors["2026"] = [1, 2];
        data.Kids[0].DoorMessages[3] = "Ahoj";

        store.Save(data);
        var loaded = new JsonFamilyStore(_directory).Load();

        loaded.Kids[0].Name.Should().Be("Ema");
        loaded.Kids[0].Wishes[0].Text.Should().Be("Žluťoučký kůň");
        loaded.Kids[0].OpenedDoors["2026"].Should().Equal(1, 2);
        loaded.Kids[0].DoorMessages[3].Should().Be("Ahoj");
    }

    [TestMethod]
    public void Load_MissingFile_GivesFreshData()
    {
        new JsonFamilyStore(_directory).Load().Kids.Should().BeEmpty();
    }

    [TestMethod]
    public void Load_CorruptPrimary_RecoversFromBackup()
    {
        JsonFamilyStore store = new(_directory);
        store.Save(new FamilyData { Kids = [new Kid { Name = "Backup" }] });
        store.Save(new FamilyData { Kids = [new Kid { Name = "Primary" }] });
        File.WriteAllText(Path.Combine(_directory, "family.json"), "{ not json");

        new JsonFamilyStore(_directory).Load().Kids[0].Name.Should().Be("Backup");
    }

    [TestMethod]
    public void Load_EverythingCorrupt_GivesFreshDataInsteadOfThrowing()
    {
        File.WriteAllText(Path.Combine(_directory, "family.json"), "garbage");

        new JsonFamilyStore(_directory).Load().Kids.Should().BeEmpty();
    }
}
