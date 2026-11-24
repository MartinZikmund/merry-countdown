using System.Text.RegularExpressions;
using FluentAssertions;
using MerryCountdown.Core.Content;

namespace MerryCountdown.Core.Tests;

[TestClass]
public partial class DoorContentTests
{
    [DataRow("en")]
    [DataRow("cs")]
    [TestMethod]
    public void Library_HasContentForEveryDoorOverSeveralYears(string language)
    {
        var library = DoorLibrary.Load(language);

        for (var year = 2026; year < 2032; year++)
        {
            for (var day = 1; day <= 24; day++)
            {
                var door = library.For(day, year);
                door.Title.Should().NotBeNullOrWhiteSpace();
                door.Body.Should().NotBeNullOrWhiteSpace();
                door.Sticker.Should().NotBeNullOrWhiteSpace();
            }
        }
    }

    [DataRow("en")]
    [DataRow("cs")]
    [TestMethod]
    public void Library_NeverRepeatsATextWithinOneYear(string language)
    {
        var library = DoorLibrary.Load(language);

        for (var year = 2026; year < 2032; year++)
        {
            var bodies = Enumerable.Range(1, 24).Select(day => library.For(day, year).Body).ToList();

            bodies.Should().OnlyHaveUniqueItems();
        }
    }

    [TestMethod]
    public void Library_RotatesContentBetweenYears()
    {
        var library = DoorLibrary.Load("en");

        library.For(2, 2026).Body.Should().NotBe(library.For(2, 2027).Body);
    }

    [TestMethod]
    public void Library_UnknownLanguage_FallsBackToEnglish()
    {
        DoorLibrary.Load("xx").For(1, 2026).Body.Should().Be(DoorLibrary.Load("en").For(1, 2026).Body);
    }

    [DataRow("en")]
    [DataRow("cs")]
    [TestMethod]
    public void Library_VideoDoors_LinkOnlyToYouTube(string language)
    {
        var library = DoorLibrary.Load(language);

        var videos = Enumerable.Range(1, 24).Select(day => library.For(day, 2026)).Where(d => d.Kind == DoorKind.Video).ToList();

        videos.Should().NotBeEmpty();
        videos.Should().OnlyContain(v => v.VideoUrl!.StartsWith("https://www.youtube.com/watch?v="));
    }

    [TestMethod]
    public void Library_ChristmasEveDoor_IsTheSpecialOne()
    {
        DoorLibrary.Load("en").For(24, 2026).Kind.Should().Be(DoorKind.Finale);
    }

    [DataRow("en")]
    [DataRow("cs")]
    [TestMethod]
    public void Library_TextsStayShortEnoughForADoorAndTheBigScreen(string language)
    {
        var library = DoorLibrary.Load(language);

        AllDoors(library).Where(d => d.Kind != DoorKind.Finale).Select(d => d.Body).Should().OnlyContain(b => b.Length <= 125);
        AllDoors(library).Select(d => d.Punchline).OfType<string>().Should().OnlyContain(p => p.Length <= 45);
        AllDisplayFacts(library).Should().OnlyContain(f => f.Length <= 80);
    }

    [TestMethod]
    public void Library_BothLanguages_HaveTheSameNumberOfItemsPerKind()
    {
        foreach (var kind in new[] { DoorKind.Joke, DoorKind.Fact, DoorKind.Activity, DoorKind.Kindness, DoorKind.Video })
        {
            var en = AllDoors(DoorLibrary.Load("en")).Where(d => d.Kind == kind).Select(d => d.Body).Distinct().Count();
            var cs = AllDoors(DoorLibrary.Load("cs")).Where(d => d.Kind == kind).Select(d => d.Body).Distinct().Count();

            cs.Should().Be(en, $"{kind} counts should match");
        }

        AllDisplayFacts(DoorLibrary.Load("cs")).Should().HaveSameCount(AllDisplayFacts(DoorLibrary.Load("en")));
    }

    [TestMethod]
    public void Library_Czech_NeverLeavesAOneLetterWordOrADayNumberAtTheEndOfALine()
    {
        var library = DoorLibrary.Load("cs");
        var texts = AllDoors(library).SelectMany(d => new[] { d.Title, d.Body, d.Punchline ?? "" }).Concat(AllDisplayFacts(library));

        texts.Should().NotContain(t => LoneLetter().IsMatch(t));
    }

    [DataRow("en")]
    [DataRow("cs")]
    [TestMethod]
    public void Library_Stickers_ExistAsPictures(string language)
    {
        var dir = FindStickerFolder();
        var stickers = AllDoors(DoorLibrary.Load(language)).Select(d => d.Sticker).Distinct();

        stickers.Should().OnlyContain(s => File.Exists(Path.Combine(dir, s + ".svg")));
    }

    // Years step through the pool by 3, so 14 consecutive years reach every item.
    private static IEnumerable<DoorContent> AllDoors(DoorLibrary library) =>
        Enumerable.Range(2026, 14).SelectMany(year => Enumerable.Range(1, 24).Select(day => library.For(day, year)));

    private static IEnumerable<string> AllDisplayFacts(DoorLibrary library) =>
        Enumerable.Range(0, 366).Select(day => library.DailyFact(new DateTime(2024, 1, 1).AddDays(day))).Distinct();

    private static string FindStickerFolder()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "MerryCountdown", "Assets", "Stickers");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("Assets/Stickers not found above the test output.");
    }

    // A one-letter preposition or conjunction, or a day number like "4.", followed by a breakable space.
    [GeneratedRegex(@"(?:(?<=^|[\s„""(])[KkSsVvZzOoUuAaIi]|(?<=\d{1,2}\.)) (?=\S)")]
    private static partial Regex LoneLetter();
}
