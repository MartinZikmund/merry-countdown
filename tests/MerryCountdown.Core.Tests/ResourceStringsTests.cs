using System.Text.RegularExpressions;
using System.Xml.Linq;
using FluentAssertions;

namespace MerryCountdown.Core.Tests;

[TestClass]
public partial class ResourceStringsTests
{
    private static readonly string[] _pluralSuffixes = ["_One", "_Few", "_Many"];

    private static Dictionary<string, string> Load(string language)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "src", "MerryCountdown", "Strings")))
        {
            dir = dir.Parent;
        }

        dir.Should().NotBeNull("the tests run from inside the repository");
        var path = Path.Combine(dir!.FullName, "src", "MerryCountdown", "Strings", language, "Resources.resw");
        return XDocument.Load(path).Root!.Elements("data")
            .ToDictionary(e => (string)e.Attribute("name")!, e => (string)e.Element("value")!);
    }

    [TestMethod]
    public void Czech_HasExactlyTheEnglishKeys() =>
        Load("cs").Keys.Should().BeEquivalentTo(Load("en").Keys);

    [DataRow("en")]
    [DataRow("cs")]
    [TestMethod]
    public void PluralKeys_ComeInAllThreeForms(string language)
    {
        var keys = Load(language).Keys.ToHashSet();
        var stems = keys.Where(k => _pluralSuffixes.Any(k.EndsWith)).Select(k => k[..k.LastIndexOf('_')]).Distinct();

        foreach (var stem in stems)
        {
            keys.Should().Contain(_pluralSuffixes.Select(s => stem + s));
        }
    }

    [TestMethod]
    public void Czech_PluralFormsKeepTheirPlaceholders()
    {
        var cs = Load("cs");
        var en = Load("en");

        foreach (var key in cs.Keys.Where(k => _pluralSuffixes.Any(k.EndsWith)))
        {
            Placeholders(cs[key]).Should().BeEquivalentTo(Placeholders(en[key]), key);
        }
    }

    [TestMethod]
    public void Czech_DoesNotAssumeAGender()
    {
        var gendered = Load("cs").Where(p => GenderedEnding().IsMatch(p.Value)).Select(p => p.Key);

        gendered.Should().BeEmpty();
    }

    [TestMethod]
    public void Czech_KeepsOneLetterWordsOffTheEndOfALine()
    {
        var loose = Load("cs").Where(p => OneLetterWordThenSpace().IsMatch(p.Value)).Select(p => p.Key);

        loose.Should().BeEmpty("a non-breaking space must follow k, s, v, z, o, u, a and i");
    }

    [TestMethod]
    public void Czech_HasNoStraightQuotes() =>
        Load("cs").Where(p => p.Value.Contains('"')).Should().BeEmpty();

    private static IEnumerable<string> Placeholders(string text) =>
        PlaceholderPattern().Matches(text).Select(m => m.Value).Distinct();

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex PlaceholderPattern();

    [GeneratedRegex(@"\w\([a-zA-Z]{1,2}\)")]
    private static partial Regex GenderedEnding();

    [GeneratedRegex(@"(?<![\p{L}\p{N}])[KkSsVvZzOoUuAaIi] ")]
    private static partial Regex OneLetterWordThenSpace();
}
