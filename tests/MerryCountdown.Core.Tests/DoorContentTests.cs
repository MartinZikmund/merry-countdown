using FluentAssertions;
using MerryCountdown.Core.Content;

namespace MerryCountdown.Core.Tests;

[TestClass]
public class DoorContentTests
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
}
