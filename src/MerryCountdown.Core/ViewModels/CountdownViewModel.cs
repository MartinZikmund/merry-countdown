using System.Collections.ObjectModel;
using System.Globalization;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Infrastructure;
using MerryCountdown.Core.Season;
using MerryCountdown.Core.Store;

namespace MerryCountdown.Core.ViewModels;

public partial class CountdownViewModel : ViewModelBase
{
    private const int NearChristmasSleeps = 24;

    private readonly IFamilyService _family;
    private readonly IFullVersionService _fullVersion;
    private readonly IAppClock _clock;
    private readonly ITicker _ticker;
    private readonly IStringLocalizer _localizer;
    private readonly INavigationService _navigation;
    private readonly IDisplayModeService _display;

    public CountdownViewModel(
        IFamilyService family,
        IFullVersionService fullVersion,
        IAppClock clock,
        ITicker ticker,
        IStringLocalizer localizer,
        INavigationService navigation,
        IDisplayModeService display)
    {
        _family = family;
        _fullVersion = fullVersion;
        _clock = clock;
        _ticker = ticker;
        _localizer = localizer;
        _navigation = navigation;
        _display = display;
        PageTitle = localizer["NavCountdown"];
        Reload();
    }

    public ObservableCollection<SceneOptionViewModel> Scenes { get; } = [];

    public ObservableCollection<UpcomingEventViewModel> Events { get; } = [];

    [ObservableProperty]
    public partial string Greeting { get; set; } = "";

    [ObservableProperty]
    public partial string SleepsNumber { get; set; } = "";

    [ObservableProperty]
    public partial string SleepsCaption { get; set; } = "";

    [ObservableProperty]
    public partial string Headline { get; set; } = "";

    [ObservableProperty]
    public partial bool IsCounting { get; set; }

    [ObservableProperty]
    public partial bool IsCelebrating { get; set; }

    [ObservableProperty]
    public partial string Days { get; set; } = "";

    [ObservableProperty]
    public partial string Hours { get; set; } = "";

    [ObservableProperty]
    public partial string Minutes { get; set; } = "";

    [ObservableProperty]
    public partial string Seconds { get; set; } = "";

    [ObservableProperty]
    public partial bool HasDoorWaiting { get; set; }

    [ObservableProperty]
    public partial string DoorButtonText { get; set; } = "";

    [ObservableProperty]
    public partial string SceneId { get; set; } = "";

    [ObservableProperty]
    public partial SnowLevel Snow { get; set; }

    [ObservableProperty]
    public partial bool CalmMotion { get; set; }

    [ObservableProperty]
    public partial bool HasEvents { get; set; }

    public override void ViewLoaded()
    {
        _ticker.Second += OnSecond;
        _family.Changed += OnFamilyChanged;
        _fullVersion.Changed += OnFamilyChanged;
        Reload();
    }

    public override void ViewUnloaded()
    {
        _ticker.Second -= OnSecond;
        _family.Changed -= OnFamilyChanged;
        _fullVersion.Changed -= OnFamilyChanged;
    }

    public void Refresh()
    {
        var now = _clock.Now;
        var settings = _family.Settings;
        var state = CountdownClock.Compute(now, settings.BigDay, settings.ArrivalMinutes);

        IsCelebrating = state.Phase == CountdownPhase.Celebrating;
        IsCounting = !IsCelebrating;
        SleepsNumber = state.Sleeps.ToString(CultureInfo.CurrentCulture);
        SleepsCaption = _localizer.Plural("SleepsCaption", state.Sleeps);
        Headline = BuildHeadline(state);

        Days = ((int)state.Remaining.TotalDays).ToString(CultureInfo.CurrentCulture);
        Hours = state.Remaining.Hours.ToString("00", CultureInfo.CurrentCulture);
        Minutes = state.Remaining.Minutes.ToString("00", CultureInfo.CurrentCulture);
        Seconds = state.Remaining.Seconds.ToString("00", CultureInfo.CurrentCulture);

        var door = AdventCalendar.TodaysDoor(now);
        var year = AdventCalendar.SeasonYear(now);
        HasDoorWaiting = door is { } today && !_family.IsDoorOpened(_family.ActiveKid, year, today);
        DoorButtonText = door is { } number ? _localizer["OpenDoorButton", number] : "";
    }

    [RelayCommand]
    private void OpenTodaysDoor() => _navigation.Navigate<CalendarViewModel>();

    [RelayCommand]
    private void NextScene()
    {
        _family.Settings.SceneId = SceneCatalog.Next(SceneId, _fullVersion.IsFull).Id;
        _family.SaveSettings();
    }

    [RelayCommand]
    private void SelectScene(SceneOptionViewModel option)
    {
        if (option.IsLocked)
        {
            _navigation.Navigate<SettingsViewModel>();
            return;
        }

        _family.Settings.SceneId = option.Id;
        _family.SaveSettings();
    }

    [RelayCommand]
    private void EnterFamilyDisplay() => _display.Enter();

    private void OnSecond(object? sender, EventArgs e) => Refresh();

    private void OnFamilyChanged(object? sender, EventArgs e) => Reload();

    private void Reload()
    {
        var settings = _family.Settings;
        SceneId = SceneCatalog.Resolve(settings.SceneId, _fullVersion.IsFull).Id;
        Snow = settings.Snow;
        CalmMotion = settings.CalmMotion;

        var name = _family.ActiveKid.Name;
        Greeting = name.Length > 0 ? _localizer["GreetingNamed", name] : _localizer["Greeting"];

        Scenes.Clear();
        foreach (var scene in SceneCatalog.All)
        {
            Scenes.Add(new SceneOptionViewModel(scene.Id, _localizer[$"Scene_{scene.Id}"], scene.RequiresFullVersion && !_fullVersion.IsFull, scene.Id == SceneId));
        }

        Events.Clear();
        if (_fullVersion.IsFull)
        {
            var now = _clock.Now;
            foreach (var row in _family.Events
                .Select(e => (Event: e, Date: CountdownClock.NextOccurrence(now, e.Month, e.Day)))
                .OrderBy(row => row.Date)
                .Take(3))
            {
                var sleeps = (row.Date - now.Date).Days;
                Events.Add(new UpcomingEventViewModel(row.Event.Name, row.Event.Sticker, sleeps == 0 ? _localizer["EventToday"] : _localizer.Plural("EventInSleeps", sleeps)));
            }
        }

        HasEvents = Events.Count > 0;
        Refresh();
    }

    private string BuildHeadline(CountdownState state)
    {
        var bigDay = _localizer.BigDayName(_family.Settings.BigDay);
        var bringer = _localizer.BringerName(_family.Settings.GiftBringer);
        return state switch
        {
            { Phase: CountdownPhase.Celebrating } => _localizer["HeadlineCelebrating"],
            { Sleeps: 0 } => _localizer["HeadlineToday", bringer],
            { Sleeps: 1 } => _localizer["HeadlineOneSleep", bigDay],
            { Sleeps: <= NearChristmasSleeps } => _localizer["HeadlineNear", bigDay],
            _ => _localizer["HeadlineFar", bigDay],
        };
    }
}

public sealed partial record SceneOptionViewModel(string Id, string Name, bool IsLocked, bool IsSelected);

public sealed partial record UpcomingEventViewModel(string Name, string Sticker, string When);
