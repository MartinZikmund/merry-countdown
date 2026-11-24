using System.Xml.Linq;
using FluentAssertions;
using MerryCountdown.Core.Family;

namespace MerryCountdown.Core.Tests;

[TestClass]
public class StickerCatalogTests
{
    // Saved wishes store these keys, so they must never disappear from the picker.
    private static readonly string[] OriginalKeys =
        ["teddy", "bike", "ball", "game", "puppy", "kitten", "bricks", "train", "robot", "crown", "book", "paints", "skates", "candy"];

    private static string AppFolder()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !Directory.Exists(Path.Combine(folder.FullName, "src", "MerryCountdown")))
        {
            folder = folder.Parent;
        }

        folder.Should().NotBeNull("the tests run from inside the repository");
        return Path.Combine(folder!.FullName, "src", "MerryCountdown");
    }

    private static HashSet<string> ResourceNames(string language) =>
        XDocument.Load(Path.Combine(AppFolder(), "Strings", language, "Resources.resw"))
            .Descendants("data")
            .Select(d => (string)d.Attribute("name")!)
            .ToHashSet();

    [TestMethod]
    public void Keys_IncludeEveryOriginalSticker()
    {
        StickerCatalog.Keys.Should().Contain(OriginalKeys);
    }

    [TestMethod]
    public void Keys_OfferAClearlyLargerSetThanTheOriginalFourteen()
    {
        StickerCatalog.Keys.Count.Should().BeGreaterThanOrEqualTo(40);
    }

    [TestMethod]
    public void Keys_AreUnique()
    {
        StickerCatalog.Keys.Should().OnlyHaveUniqueItems();
    }

    [TestMethod]
    public void Groups_AreNotEmpty()
    {
        StickerCatalog.Groups.Should().NotBeEmpty();
        StickerCatalog.Groups.Should().OnlyContain(g => g.Stickers.Count > 0);
    }

    [TestMethod]
    public void Keys_FollowTheGroupOrder()
    {
        StickerCatalog.Keys.Should().Equal(StickerCatalog.Groups.SelectMany(g => g.Stickers));
    }

    [TestMethod]
    public void EveryKey_HasAPictureInTheAssets()
    {
        foreach (var key in StickerCatalog.Keys)
        {
            File.Exists(Path.Combine(AppFolder(), "Assets", "Stickers", key + ".svg")).Should().BeTrue($"{key} needs a drawing");
        }
    }

    [TestMethod]
    [DataRow("en")]
    [DataRow("cs")]
    public void EveryKeyAndGroup_HasANameInEveryLanguage(string language)
    {
        var names = ResourceNames(language);

        foreach (var key in StickerCatalog.Keys)
        {
            names.Should().Contain($"Sticker_{key}");
        }

        foreach (var group in StickerCatalog.Groups)
        {
            names.Should().Contain($"StickerGroup_{group.Key}");
        }
    }
}
