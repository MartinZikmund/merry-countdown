using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;

namespace MerryCountdown.Core.Tests.Packaging;

// One name per language, on every head. See docs/app-name.md.
[TestClass]
public class AppNameTests
{
    private const string EnglishName = "Merry Countdown";
    private const string CzechName = "Čekání na Vánoce";

    [TestMethod]
    public void ApplicationName_InEnglishResources_IsMerryCountdown() =>
        ReadResw("en", "ApplicationName").Should().Be(EnglishName);

    [TestMethod]
    public void ApplicationName_InCzechResources_IsCekaniNaVanoce() =>
        ReadResw("cs", "ApplicationName").Should().Be(CzechName);

    [TestMethod]
    public void ApplicationName_InCzechResources_DoesNotUseTheOldName()
    {
        var cs = File.ReadAllText(Path.Combine(Root(), "src", "MerryCountdown", "Strings", "cs", "Resources.resw"));

        cs.Should().NotContain("Veselé odpočítávání").And.NotContain("Veselého odpočítávání").And.NotContain("<value>Vánoční odpočítávání</value>");
    }

    [TestMethod]
    public void AndroidLabel_PerLanguage_MatchesTheAppName()
    {
        AndroidLabel("values").Should().Be(EnglishName);
        AndroidLabel("values-cs").Should().Be(CzechName);
    }

    [TestMethod]
    public void IosDisplayName_PerLanguage_MatchesTheAppName()
    {
        IosValue("en", "CFBundleDisplayName").Should().Be(EnglishName);
        IosValue("cs", "CFBundleDisplayName").Should().Be(CzechName);
    }

    [TestMethod]
    public void WindowsManifest_PointsDisplayNameAtTheGeneratedResource()
    {
        // The manifest is generated at build time, so the rule lives in the target that rewrites it.
        var targets = File.ReadAllText(Path.Combine(Root(), "src", "Directory.Build.targets"));

        targets.Should().Contain("ms-resource:AppName/AppDisplayName");
    }

    private static string ReadResw(string lang, string key)
    {
        var doc = XDocument.Load(Path.Combine(Root(), "src", "MerryCountdown", "Strings", lang, "Resources.resw"));
        return doc.Root!.Elements("data").Single(e => (string?)e.Attribute("name") == key).Element("value")!.Value;
    }

    private static string AndroidLabel(string folder)
    {
        var doc = XDocument.Load(Path.Combine(Root(), "src", "MerryCountdown", "Platforms", "Android", "Resources", folder, "Strings.xml"));
        return doc.Root!.Elements("string").Single(e => (string?)e.Attribute("name") == "ApplicationName").Value;
    }

    private static string IosValue(string lang, string key)
    {
        var text = File.ReadAllText(Path.Combine(Root(), "src", "MerryCountdown", "Platforms", "iOS", "Resources", $"{lang}.lproj", "InfoPlist.strings"));
        return Regex.Match(text, $"\"{key}\"\\s*=\\s*\"([^\"]*)\"").Groups[1].Value;
    }

    private static string Root()
    {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new DirectoryNotFoundException("Repository root (AGENTS.md) not found.");
    }
}
