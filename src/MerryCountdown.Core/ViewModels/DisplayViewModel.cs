using System.Collections.ObjectModel;
using System.Globalization;
using MerryCountdown.Core.Content;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Infrastructure;
using MerryCountdown.Core.Season;
using MerryCountdown.Core.Store;

namespace MerryCountdown.Core.ViewModels;

/// <summary>
/// The screen for a TV or tablet left on all advent: huge, readable from the sofa,
/// and always in slow motion so nothing burns into the panel.
/// </summary>
public partial class DisplayViewModel : ViewModelBase
{
    private const int SecondsPerScene = 30 * 60;
    private const int DriftPeriodSeconds = 10 * 60;
    private const double DriftRange = 24;
    private const int HintSeconds = 8;

    private readonly IFamilyService _family;
    private readonly IFullVersionService _fullVersion;
    private readonly IAppClock _clock;
    private readonly ITicker _ticker;
    private readonly IStringLocalizer _localizer;
    private readonly IDisplayModeService _display;
    private readonly DoorLibrary _library;
    private int _secondsOnScene;
    private int _secondsRunning;
    private int _hintLeft = HintSeconds;

    public DisplayViewModel(
        IFamilyService family,
        IFullVersionService fullVersion,
        IAppClock clock,
        ITicker ticker,
        IStringLocalizer localizer,
        IDisplayModeService display,
        PhotoBackdrop backdrop)
    {
        _family = family;
        _fullVersion = fullVersion;
        _clock = clock;
        _ticker = ticker;
        _localizer = localizer;
        _display = display;
        Backdrop = backdrop;
        _library = DoorLibrary.Load(CultureInfo.CurrentUICulture.Name);
        SceneId = backdrop.SceneFor(SceneCatalog.Resolve(family.Settings.SceneId, fullVersion.IsFull).Id);
        Tick();
    }

    public PhotoBackdrop Backdrop { get; }

    public ObservableCollection<DisplayKidViewModel> Kids { get; } = [];

    [ObservableProperty]
    public partial string SceneId { get; set; } = "";

    [ObservableProperty]
    public partial SnowLevel Snow { get; set; }

    [ObservableProperty]
    public partial bool CalmMotion { get; set; }

    [ObservableProperty]
    public partial string SleepsNumber { get; set; } = "";

    [ObservableProperty]
    public partial string SleepsCaption { get; set; } = "";

    [ObservableProperty]
    public partial bool IsCelebrating { get; set; }

    [ObservableProperty]
    public partial bool IsCounting { get; set; }

    [ObservableProperty]
    public partial string Countdown { get; set; } = "";

    [ObservableProperty]
    public partial string Clock { get; set; } = "";

    [ObservableProperty]
    public partial string DailyFact { get; set; } = "";

    [ObservableProperty]
    public partial string Today { get; set; } = "";

    [ObservableProperty]
    public partial double DriftX { get; set; }

    [ObservableProperty]
    public partial double DriftY { get; set; }

    [ObservableProperty]
    public partial bool IsHintVisible { get; set; } = true;

    public void Start() => _ticker.Second += OnSecond;

    public void Stop() => _ticker.Second -= OnSecond;

    /// <summary>Any key or wiggle of the mouse brings the how-to-leave hint back.</summary>
    public void ShowHint()
    {
        _hintLeft = HintSeconds;
        IsHintVisible = true;
    }

    [RelayCommand]
    public void NextScene()
    {
        if (Backdrop.IsActive)
        {
            Backdrop.Next();
            return;
        }

        SceneId = SceneCatalog.Next(SceneId, _fullVersion.IsFull).Id;
        _secondsOnScene = 0;
    }

    [RelayCommand]
    private void Exit() => _display.Exit();

    public void Tick()
    {
        var now = _clock.Now;
        var settings = _family.Settings;
        var state = CountdownClock.Compute(now, settings.BigDay, settings.ArrivalMinutes);

        Snow = settings.Snow;
        CalmMotion = settings.CalmMotion;
        IsCelebrating = state.Phase == CountdownPhase.Celebrating;
        IsCounting = !IsCelebrating;
        SleepsNumber = state.Sleeps.ToString(CultureInfo.CurrentCulture);
        SleepsCaption = _localizer.Plural("SleepsCaption", state.Sleeps);
        Countdown = _localizer["DisplayCountdown", (int)state.Remaining.TotalDays, state.Remaining.Hours, state.Remaining.Minutes.ToString("00", CultureInfo.CurrentCulture), state.Remaining.Seconds.ToString("00", CultureInfo.CurrentCulture)];
        Clock = now.ToString("t", CultureInfo.CurrentCulture);
        Today = now.ToString("M", CultureInfo.CurrentCulture);
        DailyFact = _library.DailyFact(now);

        var angle = (_secondsRunning % DriftPeriodSeconds) * Math.Tau / DriftPeriodSeconds;
        DriftX = Math.Sin(angle) * DriftRange;
        DriftY = Math.Sin(angle * 2) * DriftRange * 0.5;

        UpdateKids(now);
    }

    private void OnSecond(object? sender, EventArgs e)
    {
        _secondsRunning++;
        _secondsOnScene++;

        if (_family.Settings.DisplayRotatesScenes && !Backdrop.IsActive && _secondsOnScene >= SecondsPerScene)
        {
            NextScene();
        }

        if (_hintLeft > 0 && --_hintLeft == 0)
        {
            IsHintVisible = false;
        }

        Tick();
    }

    private void UpdateKids(DateTime now)
    {
        var door = AdventCalendar.TodaysDoor(now);
        var year = AdventCalendar.SeasonYear(now);
        var kids = _family.Kids;

        if (Kids.Count != kids.Count || Kids.Zip(kids).Any(p => p.First.Kid != p.Second))
        {
            Kids.Clear();
            foreach (var kid in kids)
            {
                Kids.Add(new DisplayKidViewModel(kid));
            }
        }

        foreach (var row in Kids)
        {
            row.Update(door, year, _family, _localizer);
        }
    }
}

public partial class DisplayKidViewModel(Kid kid) : ObservableObject
{
    public Kid Kid { get; } = kid;

    public string Sticker => Kid.Sticker;

    [ObservableProperty]
    public partial string Name { get; set; } = "";

    [ObservableProperty]
    public partial string Status { get; set; } = "";

    internal void Update(int? door, int year, IFamilyService family, IStringLocalizer localizer)
    {
        Name = Kid.Name.Length > 0 ? Kid.Name : localizer["KidNoName"];

        var wishes = localizer.Plural("DisplayWishes", Kid.Wishes.Count);
        Status = door is { } today
            ? (family.IsDoorOpened(Kid, year, today) ? localizer["DisplayDoorOpened", today] : localizer["DisplayDoorWaiting", today]) + " · " + wishes
            : wishes;
    }
}
