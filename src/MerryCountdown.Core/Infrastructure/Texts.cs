using System.Globalization;
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

    /// <summary>The family's big day by name, or by date when it is one of their own.</summary>
    public static string BigDayName(this IStringLocalizer localizer, FamilySettings settings) =>
        settings.BigDay == BigDay.Custom
            ? new DateTime(2028, settings.CustomBigDayMonth, settings.CustomBigDayDay).ToString("M", CultureInfo.CurrentCulture)
            : localizer.BigDayName(settings.BigDay);
}
