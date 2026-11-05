using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MerryCountdown.Core.Content;

public enum DoorKind
{
    Joke,
    Fact,
    Activity,
    Kindness,
    Video,
    Finale,
}

/// <param name="Punchline">Only jokes have one; it stays hidden until the kid asks for it.</param>
/// <param name="VideoUrl">Only video doors have one; it opens outside the app, behind the parent gate.</param>
public sealed partial record DoorContent(DoorKind Kind, string Title, string Body, string Sticker, string? Punchline = null, string? VideoUrl = null);

/// <summary>Everything behind the doors ships inside the app: no network, no ads, no surprises for parents.</summary>
public sealed class DoorLibrary
{
    private const int YearStride = 3;
    private const string DefaultLanguage = "en";

    // Door 24 is always the finale, so this schedule covers doors 1-23.
    private static readonly DoorKind[] _schedule =
    [
        DoorKind.Joke, DoorKind.Activity, DoorKind.Fact, DoorKind.Video,
        DoorKind.Joke, DoorKind.Kindness, DoorKind.Fact, DoorKind.Activity,
        DoorKind.Joke, DoorKind.Video, DoorKind.Fact, DoorKind.Kindness,
        DoorKind.Activity, DoorKind.Joke, DoorKind.Fact, DoorKind.Video,
        DoorKind.Joke, DoorKind.Activity, DoorKind.Fact, DoorKind.Kindness,
        DoorKind.Joke, DoorKind.Video, DoorKind.Activity,
    ];

    private readonly DoorFile _file;

    private DoorLibrary(DoorFile file) => _file = file;

    public static DoorLibrary Load(string language)
    {
        var file = TryRead(language.Split('-')[0].ToLowerInvariant()) ?? TryRead(DefaultLanguage)
            ?? throw new InvalidOperationException("The default door content is missing from the assembly.");
        return new DoorLibrary(file);
    }

    public DoorContent For(int door, int year)
    {
        if (door is < 1 or > 24)
        {
            throw new ArgumentOutOfRangeException(nameof(door));
        }

        if (door == 24)
        {
            return new DoorContent(DoorKind.Finale, _file.Titles.Finale, _file.Finale, "star");
        }

        var kind = _schedule[door - 1];
        var slot = _schedule.Take(door - 1).Count(k => k == kind);

        return kind switch
        {
            DoorKind.Joke => Build(kind, _file.Titles.Joke, _file.Jokes, slot, year),
            DoorKind.Fact => Build(kind, _file.Titles.Fact, _file.Facts, slot, year),
            DoorKind.Activity => Build(kind, _file.Titles.Activity, _file.Activities, slot, year),
            DoorKind.Kindness => Build(kind, _file.Titles.Kindness, _file.Kindness, slot, year),
            DoorKind.Video => Build(kind, _file.Titles.Video, _file.Videos, slot, year),
            _ => throw new InvalidOperationException($"Unknown door kind {kind}."),
        };
    }

    /// <summary>One thing to say on the big screen each day, without spoiling what's behind a door.</summary>
    public string DailyFact(DateTime date) => _file.DisplayFacts[date.DayOfYear % _file.DisplayFacts.Count];

    private static DoorContent Build(DoorKind kind, string title, IReadOnlyList<DoorItem> pool, int slot, int year)
    {
        var item = pool[(slot + (year * YearStride)) % pool.Count];
        var url = item.Video is null ? null : $"https://www.youtube.com/watch?v={item.Video}";
        return new DoorContent(kind, title, item.Text, item.Sticker, item.Punchline, url);
    }

    private static DoorFile? TryRead(string language)
    {
        var assembly = typeof(DoorLibrary).GetTypeInfo().Assembly;
        var name = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith($"doors.{language}.json", StringComparison.Ordinal));
        if (name is null)
        {
            return null;
        }

        using var stream = assembly.GetManifestResourceStream(name)!;
        return JsonSerializer.Deserialize(stream, DoorJsonContext.Default.DoorFile);
    }
}

internal sealed class DoorFile
{
    public DoorTitles Titles { get; set; } = new();

    public string Finale { get; set; } = "";

    public List<DoorItem> Jokes { get; set; } = [];

    public List<DoorItem> Facts { get; set; } = [];

    public List<DoorItem> Activities { get; set; } = [];

    public List<DoorItem> Kindness { get; set; } = [];

    public List<DoorItem> Videos { get; set; } = [];

    public List<string> DisplayFacts { get; set; } = [];
}

internal sealed class DoorTitles
{
    public string Joke { get; set; } = "";

    public string Fact { get; set; } = "";

    public string Activity { get; set; } = "";

    public string Kindness { get; set; } = "";

    public string Video { get; set; } = "";

    public string Finale { get; set; } = "";
}

internal sealed class DoorItem
{
    public string Text { get; set; } = "";

    public string Sticker { get; set; } = "star";

    public string? Punchline { get; set; }

    public string? Video { get; set; }
}

[JsonSerializable(typeof(DoorFile))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal sealed partial class DoorJsonContext : JsonSerializerContext;
