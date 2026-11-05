using System.Collections.ObjectModel;
using System.Globalization;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Infrastructure;
using MerryCountdown.Core.Parents;
using MerryCountdown.Core.Season;
using MerryCountdown.Core.Store;

namespace MerryCountdown.Core.ViewModels;

public partial class CountdownViewModel : ViewModelBase
{
    public const string PhotosOptionId = "photos";

    private const string SceneWithTrainOnTheLeft = "train";

    private readonly IFamilyService _family;
    private readonly IFullVersionService _fullVersion;
    private readonly IAppClock _clock;
    private readonly ITicker _ticker;
    private readonly IStringLocalizer _localizer;
    private readonly INavigationService _navigation;
    private readonly IDisplayModeService _display;
    private readonly IParentGateService _gate;
    private readonly ISoundService _sounds;
    private readonly ICompactOverlayService _overlay;

    public CountdownViewModel(
        IFamilyService family,
        IFullVersionService fullVersion,
        IAppClock clock,
        ITicker ticker,
        IStringLocalizer localizer,
        INavigationService navigation,
        IDisplayModeService display,
        PhotoBackdrop backdrop,
        IParentGateService gate,
        ISoundService sounds,
        ICompactOverlayService overlay)
    {
        _sounds = sounds;
        _overlay = overlay;
        _family = family;
        _fullVersion = fullVersion;
        _clock = clock;
        _ticker = ticker;
        _localizer = localizer;
        _navigation = navigation;
        _display = display;
        _gate = gate;
        Backdrop = backdrop;
        Sign = new CountdownSignViewModel(localizer);
        SignOptions = [.. Enum.GetValues<SignDesign>().Select(d => new SignOptionViewModel(d, localizer[$"Sign_{d}"], CountdownSignViewModel.Preview(d, CountdownStyle.Plates, localizer)))];
        StyleOptions = [.. Enum.GetValues<CountdownStyle>().Select(k => new StyleOptionViewModel(k, localizer[$"Style_{k}"], CountdownSignViewModel.Preview(SignDesign.Plank, k, localizer)))];
        PageTitle = localizer["NavCountdown"];
        Reload();
    }

    public PhotoBackdrop Backdrop { get; }

    public CountdownSignViewModel Sign { get; }

    public IReadOnlyList<SignOptionViewModel> SignOptions { get; }

    public IReadOnlyList<StyleOptionViewModel> StyleOptions { get; }

    public ObservableCollection<SceneOptionViewModel> Scenes { get; } = [];

    public ObservableCollection<UpcomingEventViewModel> Events { get; } = [];

    [ObservableProperty]
    public partial HomeDoorStage DoorStage { get; set; }

    [ObservableProperty]
    public partial int DoorNumber { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDoorCaption))]
    public partial string DoorCaption { get; set; } = "";

    public bool HasDoorCaption => DoorCaption.Length > 0;

    [ObservableProperty]
    public partial bool CanWriteWishLetter { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDoorOnRight))]
    public partial string SceneId { get; set; } = "";

    /// <summary>The toy train runs along the left, so the door stands on the other side there.</summary>
    public bool IsDoorOnRight => SceneId == SceneWithTrainOnTheLeft;

    [ObservableProperty]
    public partial SnowLevel Snow { get; set; }

    [ObservableProperty]
    public partial bool CalmMotion { get; set; }

    [ObservableProperty]
    public partial bool HasEvents { get; set; }

    public bool CanUseCompactOverlay => _overlay.IsSupported;

    /// <summary>The overlay covers this page, so painting its scene as well would only drain the battery.</summary>
    [ObservableProperty]
    public partial bool IsScenePaused { get; set; }

    public override void ViewLoaded()
    {
        _overlay.Changed += OnOverlayChanged;
        _ticker.Second += OnSecond;
        _family.Changed += OnFamilyChanged;
        _fullVersion.Changed += OnFamilyChanged;
        Reload();
    }

    public override void ViewUnloaded()
    {
        _overlay.Changed -= OnOverlayChanged;
        _ticker.Second -= OnSecond;
        _family.Changed -= OnFamilyChanged;
        _fullVersion.Changed -= OnFamilyChanged;
    }

    public void Refresh()
    {
        var now = _clock.Now;
        Sign.Update(now, _family.Settings);
        UpdateDoor(now);
    }

    public void DoorStartedSwinging() => _sounds.Play(SoundEffect.DoorOpen);

    public void LockedDoorTapped() => _sounds.Play(SoundEffect.DoorLocked);

    public void SceneTapped() => _sounds.Play(SoundEffect.SceneTap);

    [RelayCommand]
    private void OpenTodaysDoor() => _navigation.Navigate<CalendarViewModel>(DoorNumber);

    [RelayCommand]
    private void WriteWishLetter() => _navigation.Navigate<WishListViewModel>();

    [RelayCommand]
    private void SelectSign(SignOptionViewModel option)
    {
        _family.Settings.Sign = option.Design;
        _family.SaveSettings();
    }

    [RelayCommand]
    private void SelectStyle(StyleOptionViewModel option)
    {
        _family.Settings.CountdownStyle = option.Style;
        _family.SaveSettings();
    }

    [RelayCommand]
    private async Task SelectSceneAsync(SceneOptionViewModel option)
    {
        if (option.Id == PhotosOptionId)
        {
            await SelectPhotosAsync();
            return;
        }

        if (option.IsLocked)
        {
            _navigation.Navigate<SettingsViewModel>();
            return;
        }

        _family.Settings.UsesPhotos = false;
        _family.Settings.SceneId = option.Id;
        _family.SaveSettings();
    }

    private async Task SelectPhotosAsync()
    {
        if (_fullVersion.IsFull && _family.Settings.PhotoIds.Count > 0)
        {
            _family.Settings.UsesPhotos = true;
            _family.SaveSettings();
        }
        else if (await _gate.PassAsync())
        {
            // Nothing to show yet, or not bought yet. Both are for a grown-up.
            _navigation.Navigate<ParentZoneViewModel>();
        }
    }

    [RelayCommand]
    private void EnterFamilyDisplay() => _display.Enter();

    [RelayCommand]
    private void EnterCompactOverlay() => _overlay.Enter();

    private void OnOverlayChanged(object? sender, EventArgs e) => IsScenePaused = _overlay.IsActive;

    private void OnSecond(object? sender, EventArgs e) => Refresh();

    private void OnFamilyChanged(object? sender, EventArgs e) => Reload();

    private void Reload()
    {
        var settings = _family.Settings;
        var chosenScene = SceneCatalog.Resolve(settings.SceneId, _fullVersion.IsFull).Id;
        SceneId = Backdrop.SceneFor(chosenScene);
        Snow = settings.Snow;
        CalmMotion = settings.CalmMotion;

        foreach (var option in SignOptions)
        {
            option.IsSelected = option.Design == settings.Sign;
        }

        foreach (var option in StyleOptions)
        {
            option.IsSelected = option.Style == settings.CountdownStyle;
        }

        Scenes.Clear();
        foreach (var scene in SceneCatalog.All)
        {
            Scenes.Add(new SceneOptionViewModel(scene.Id, _localizer[$"Scene_{scene.Id}"], scene.RequiresFullVersion && !_fullVersion.IsFull, scene.Id == SceneId));
        }

        Scenes.Add(new SceneOptionViewModel(PhotosOptionId, _localizer["Scene_photos"], !_fullVersion.IsFull, Backdrop.IsActive));

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

    private void UpdateDoor(DateTime now)
    {
        var door = AdventCalendar.TodaysDoor(now);
        if (door is { } today)
        {
            var isOpened = _family.IsDoorOpened(_family.ActiveKid, AdventCalendar.SeasonYear(now), today);
            DoorStage = isOpened ? HomeDoorStage.Opened : HomeDoorStage.Waiting;
            DoorNumber = today;
            DoorCaption = !isOpened
                ? _localizer["OpenDoorButton", today]
                : today < AdventCalendar.DoorCount ? _localizer["DoorSeeYouTomorrow"] : "";
            CanWriteWishLetter = false;
            return;
        }

        if (Sign.IsCelebrating)
        {
            DoorStage = HomeDoorStage.Hidden;
            DoorCaption = "";
            CanWriteWishLetter = false;
            return;
        }

        var firstDoor = new DateTime(now.Month == 12 ? now.Year + 1 : now.Year, 12, 1);
        DoorStage = HomeDoorStage.Locked;
        DoorNumber = 1;
        DoorCaption = _localizer.Plural("DoorOneOpensIn", (firstDoor - now.Date).Days);
        CanWriteWishLetter = true;
    }
}

public enum HomeDoorStage
{
    Hidden,
    Locked,
    Waiting,
    Opened,
}

public sealed partial class SignOptionViewModel(SignDesign design, string name, CountdownSignViewModel preview) : ObservableObject
{
    public SignDesign Design { get; } = design;

    public string Name { get; } = name;

    public CountdownSignViewModel Preview { get; } = preview;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

public sealed partial class StyleOptionViewModel(CountdownStyle style, string name, CountdownSignViewModel preview) : ObservableObject
{
    public CountdownStyle Style { get; } = style;

    public string Name { get; } = name;

    public CountdownSignViewModel Preview { get; } = preview;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

public sealed partial record SceneOptionViewModel(string Id, string Name, bool IsLocked, bool IsSelected);

public sealed partial record UpcomingEventViewModel(string Name, string Sticker, string When);
