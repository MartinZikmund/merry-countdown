using System.Text.RegularExpressions;
using FluentAssertions;
using MerryCountdown.Core.Services;

namespace MerryCountdown.Core.Tests;

[TestClass]
public partial class SoundFilesTests
{
    private static string SoundsFolder()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !Directory.Exists(Path.Combine(folder.FullName, "src", "MerryCountdown")))
        {
            folder = folder.Parent;
        }

        folder.Should().NotBeNull("the tests run from inside the repository");
        return Path.Combine(folder!.FullName, "src", "MerryCountdown", "Assets", "Sounds");
    }

    [TestMethod]
    public void NameOf_EveryEffect_HasAFileToPlay()
    {
        foreach (var effect in Enum.GetValues<SoundEffect>())
        {
            File.Exists(Path.Combine(SoundsFolder(), SoundFiles.NameOf(effect))).Should().BeTrue($"{effect} needs a sound file");
        }
    }

    // Android packaging turns "-" into "_" but the player opens the original name, so the sound silently fails.
    [TestMethod]
    public void SoundFolder_FileNames_SurviveAndroidPackaging()
    {
        foreach (var file in Directory.GetFiles(SoundsFolder()).Select(Path.GetFileName))
        {
            file.Should().MatchRegex(AndroidSafeName().ToString());
        }
    }

    [GeneratedRegex("^[a-z_]+\\.mp3$")]
    private static partial Regex AndroidSafeName();
}
