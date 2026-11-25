using System.Globalization;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Infrastructure;
using MerryCountdown.Core.Season;

namespace MerryCountdown.Core.ViewModels;

/// <summary>What the hanging sign shows. Home and Family Display share it, so they always agree.</summary>
public partial class CountdownSignViewModel(IStringLocalizer localizer) : ObservableObject
{
    private static readonly DateTime PreviewMoment = new(2026, 12, 9, 18, 30, 12);

    [ObservableProperty]
    public partial SignDesign Design { get; set; }

    [ObservableProperty]
    public partial CountdownStyle Style { get; set; }

    [ObservableProperty]
    public partial bool IsCelebrating { get; set; }

    [ObservableProperty]
    public partial bool IsCounting { get; set; }

    [ObservableProperty]
    public partial string CelebrationText { get; set; } = "";

    [ObservableProperty]
    public partial string SleepsNumber { get; set; } = "";

    [ObservableProperty]
    public partial string SleepsCaption { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNextChangeCaption))]
    public partial string NextChangeCaption { get; set; } = "";

    public bool HasNextChangeCaption => NextChangeCaption.Length > 0;

    [ObservableProperty]
    public partial string ClockHours { get; set; } = "";

    [ObservableProperty]
    public partial string ClockMinutes { get; set; } = "";

    [ObservableProperty]
    public partial string ClockSeconds { get; set; } = "";

    [ObservableProperty]
    public partial string Days { get; set; } = "";

    [ObservableProperty]
    public partial string Hours { get; set; } = "";

    [ObservableProperty]
    public partial string Minutes { get; set; } = "";

    [ObservableProperty]
    public partial string Seconds { get; set; } = "";

    [ObservableProperty]
    public partial string DaysLabel { get; set; } = "";

    [ObservableProperty]
    public partial string HoursLabel { get; set; } = "";

    [ObservableProperty]
    public partial string MinutesLabel { get; set; } = "";

    [ObservableProperty]
    public partial string SecondsLabel { get; set; } = "";

    /// <summary>One sentence for screen readers. Leaves out the seconds so it doesn't chatter.</summary>
    [ObservableProperty]
    public partial string AccessibleName { get; set; } = "";

    public static CountdownSignViewModel Preview(SignDesign design, CountdownStyle style, IStringLocalizer localizer)
    {
        CountdownSignViewModel sign = new(localizer);
        FamilySettings settings = new() { Sign = design, CountdownStyle = style };
        settings.UseBigDay(BigDay.ChristmasDay);
        sign.Update(PreviewMoment, settings);
        return sign;
    }

    public void Update(DateTime now, FamilySettings settings)
    {
        var (month, day) = settings.BigDate();
        var state = CountdownClock.Compute(now, month, day, settings.ArrivalMinutes);
        var bigDay = localizer.BigDayName(settings);
        var remaining = state.Remaining;
        var days = (int)remaining.TotalDays;

        Design = settings.Sign;
        Style = settings.CountdownStyle;
        IsCelebrating = state.Phase == CountdownPhase.Celebrating;
        IsCounting = !IsCelebrating;
        CelebrationText = localizer["HeadlineCelebrating"];

        SleepsNumber = state.Sleeps.ToString(CultureInfo.CurrentCulture);
        SleepsCaption = localizer.Plural("SleepsCaption", state.Sleeps);
        NextChangeCaption = state.Sleeps switch
        {
            0 => localizer["SignNextChangeBigDay"],
            1 => localizer["SignNextChangeLastSleep", bigDay],

            // On an ordinary day the sleeps and the clock say enough.
            _ => "",
        };
        ClockHours = TwoDigits((int)state.UntilNextSleep.TotalHours);
        ClockMinutes = TwoDigits(state.UntilNextSleep.Minutes);
        ClockSeconds = TwoDigits(state.UntilNextSleep.Seconds);

        Days = TwoDigits(days);
        Hours = TwoDigits(remaining.Hours);
        Minutes = TwoDigits(remaining.Minutes);
        Seconds = TwoDigits(remaining.Seconds);
        DaysLabel = localizer.Plural("UnitDays", days);
        HoursLabel = localizer.Plural("UnitHours", remaining.Hours);
        MinutesLabel = localizer.Plural("UnitMinutes", remaining.Minutes);
        SecondsLabel = localizer.Plural("UnitSeconds", remaining.Seconds);

        AccessibleName = (IsCelebrating, Style) switch
        {
            (true, _) => CelebrationText,
            (_, CountdownStyle.SleepsAndClock) => localizer["SignAccessible_Sleeps", state.Sleeps, SleepsCaption, bigDay],
            _ => localizer["SignAccessible_Plates", days, DaysLabel, remaining.Hours, HoursLabel, remaining.Minutes, MinutesLabel, bigDay],
        };
    }

    private static string TwoDigits(int value) => value.ToString("00", CultureInfo.CurrentCulture);
}
