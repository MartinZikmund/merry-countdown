using MerryCountdown.Core.Family;
using MerryCountdown.Core.Season;

namespace MerryCountdown.Core.Infrastructure;

public static class Texts
{
    /// <summary>Picks "_One", "_Few" or "_Many". Only Czech-style plurals need Few; other languages repeat Many.</summary>
    public static string Plural(this IStringLocalizer localizer, string key, int count) =>
        localizer[$"{key}_{count switch { 1 => "One", >= 2 and <= 4 => "Few", _ => "Many" }}", count];

    public static string BringerName(this IStringLocalizer localizer, GiftBringer bringer) =>
        localizer[$"GiftBringer_{bringer}"];

    public static string BigDayName(this IStringLocalizer localizer, BigDay bigDay) =>
        localizer[$"BigDay_{bigDay}"];
}
