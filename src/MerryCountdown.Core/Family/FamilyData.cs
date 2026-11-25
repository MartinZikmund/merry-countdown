using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using MerryCountdown.Core.Season;

namespace MerryCountdown.Core.Family;

/// <summary>Who brings the presents. Text only: the bringer never appears on screen, so one app fits both.</summary>
public enum GiftBringer
{
    Santa,
    Jezisek,
}

public enum SnowLevel
{
    Off,
    Gentle,
    Blizzard,
}

/// <summary>The look of the countdown sign hanging over the scene.</summary>
public enum SignDesign
{
    Plank,
    Gingerbread,
    ParcelTag,
    Station,
    Baubles,
}

public enum CountdownStyle
{
    Plates,
    SleepsAndClock,
}

/// <summary>Everything the app remembers. Stored only on this device.</summary>
public sealed class FamilyData
{
    public const int CurrentVersion = 2;

    public int Version { get; set; } = CurrentVersion;

    public List<Kid> Kids { get; set; } = [];

    public string? ActiveKidId { get; set; }

    public FamilySettings? Settings { get; set; }

    public List<FamilyEvent> Events { get; set; } = [];

    public bool IsFullVersionCached { get; set; }
}

public sealed class Kid
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "";

    public string Sticker { get; set; } = "star";

    public List<Wish> Wishes { get; set; } = [];

    /// <summary>The December this kid's letter went out. Null while it is still being written.</summary>
    public int? LetterSentYear { get; set; }

    /// <summary>Opened doors, keyed by season year as a string because JSON keys are strings.</summary>
    public Dictionary<string, List<int>> OpenedDoors { get; set; } = [];

    /// <summary>Grown-ups' notes shown behind a door, keyed by door number.</summary>
    public Dictionary<int, string> DoorMessages { get; set; } = [];

    public string WishesAsText(Func<string, string> stickerName)
    {
        StringBuilder text = new();
        if (Name.Length > 0)
        {
            text.AppendLine(CultureInfo.InvariantCulture, $"{Name}:");
        }

        foreach (var wish in Wishes)
        {
            var label = wish.Text.Length > 0 ? wish.Text : stickerName(wish.Sticker ?? "");
            text.AppendLine(CultureInfo.InvariantCulture, $"- {label}");
        }

        return text.ToString();
    }
}

public sealed class Wish
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Text { get; set; } = "";

    public string? Sticker { get; set; }
}

public sealed class FamilyEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "";

    public int Month { get; set; }

    public int Day { get; set; }

    public string Sticker { get; set; } = "cake";
}

public sealed class FamilySettings
{
    public const int DefaultPhotoIntervalSeconds = 300;

    private const int EveningArrival = 17 * 60;

    public BigDay BigDay { get; set; }

    public GiftBringer GiftBringer { get; set; }

    /// <summary>Used while <see cref="BigDay"/> is <see cref="BigDay.Custom"/>.</summary>
    public int CustomBigDayMonth { get; set; } = 12;

    public int CustomBigDayDay { get; set; } = 31;

    /// <summary>Minutes after midnight on the big day when the countdown reaches zero.</summary>
    public int ArrivalMinutes { get; set; }

    public string? SceneId { get; set; }

    public SignDesign Sign { get; set; } = SignDesign.Plank;

    public CountdownStyle CountdownStyle { get; set; } = CountdownStyle.Plates;

    public SnowLevel Snow { get; set; } = SnowLevel.Gentle;

    public bool CalmMotion { get; set; }

    public bool SoundEnabled { get; set; } = true;

    public bool DisplayRotatesScenes { get; set; } = true;

    public bool StartInFamilyDisplay { get; set; }

    public bool UsesPhotos { get; set; }

    public List<string> PhotoIds { get; set; } = [];

    public int PhotoIntervalSeconds { get; set; } = DefaultPhotoIntervalSeconds;

    /// <summary>Only read from version 1 data, where photos had a foreground scene of their own. See <see cref="FamilyService"/>.</summary>
    public string? PhotoForegroundSceneId { get; set; }

    /// <summary>The grown-ups' question is never asked on this device.</summary>
    public bool KeepGrownUpsUnlocked { get; set; }

    public static FamilySettings ForCulture(string cultureName)
    {
        var language = cultureName.Split('-')[0].ToLowerInvariant();
        var eve = language is "cs" or "sk" or "de" or "pl" or "hu" or "at" or "sv" or "da" or "nb" or "no" or "fi" or "hr" or "sl" or "lt" or "lv" or "et";

        FamilySettings settings = new()
        {
            GiftBringer = language is "cs" or "sk" ? GiftBringer.Jezisek : GiftBringer.Santa,
        };
        settings.UseBigDay(eve ? BigDay.ChristmasEve : BigDay.ChristmasDay);
        return settings;
    }

    public void UseBigDay(BigDay bigDay)
    {
        BigDay = bigDay;
        ArrivalMinutes = bigDay switch
        {
            BigDay.ChristmasEve => EveningArrival,
            BigDay.ChristmasDay => 0,
            _ => ArrivalMinutes,
        };
    }

    /// <summary>The month and day the countdown runs to.</summary>
    public (int Month, int Day) BigDate() => BigDay switch
    {
        BigDay.ChristmasEve => (12, 24),
        BigDay.ChristmasDay => (12, 25),
        _ => (CustomBigDayMonth, CustomBigDayDay),
    };
}

[JsonSerializable(typeof(FamilyData))]
[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true)]
public sealed partial class FamilyJsonContext : JsonSerializerContext;
