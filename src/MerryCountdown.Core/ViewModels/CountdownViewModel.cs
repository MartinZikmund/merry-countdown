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
    public const string PhotosOptionId = SceneCatalog.PhotosId;

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
    private readonly ISnackService _messages;
    private readonly IShakeService _shakes;

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
        ICompactOverlayService overlay,
        ISnackService messages,
        IShakeService shakes)
    {
        _shakes = shakes;
        _messages = messages;
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
    public partial string SceneId { get; set; } = "";

    [ObservableProperty]
    public partial SnowLevel Snow { get; set; }

    [ObservableProperty]
    public partial bool CalmMotion { get; set; }

    [ObservableProperty]
    public partial bool HasEvents { get; set; }

    /// <summary>The family's photos show behind the scene. Always on for the Photos scene.</summary>
    [ObservableProperty]
    public partial bool PhotosBehindScene { get; set; }

    /// <summary>False while the Photos scene is chosen: there the photos are the scene.</summary>
    [ObservableProperty]
    public partial bool CanTogglePhotos { get; set; } = true;

    /// <summary>Turning photos on leads to a dialog or another page first: the full version, or adding some photos.</summary>
    public bool PhotosNeedSetup => !_fullVersion.IsFull || _family.Settings.PhotoIds.Count == 0;

    public bool CanUseCompactOverlay => _overlay.IsSupported;

    /// <summary>The overlay covers this page, so painting its scene as well would only drain the battery.</summary>
    [ObservableProperty]
    public partial bool IsScenePaused { get; set; }

    /// <summary>The latest push of a shake, for the scene to answer.</summary>
    [ObservableProperty]
    public partial ShakeEventArgs? Shake { get; private set; }

    public override void ViewLoaded()
    {
        _overlay.Changed += OnOverlayChanged;
        _ticker.Second += OnSecond;
        _family.Changed += OnFamilyChanged;
        _fullVersion.Changed += OnFamilyChanged;
        _shakes.Shaken += OnShaken;
        Reload();
    }

    public override void ViewUnloaded()
    {
        _overlay.Changed -= OnOverlayChanged;
        _ticker.Second -= OnSecond;
        _family.Changed -= OnFamilyChanged;
        _fullVersion.Changed -= OnFamilyChanged;
        _shakes.Shaken -= OnShaken;
    }

    public void Refresh() => Sign.Update(_clock.Now, _family.Settings);

    public void SceneTapped() => _sounds.Play(SoundEffect.SceneTap);

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
            await OfferFullVersionAsync();
            return;
        }

        _family.Settings.SceneId = option.Id;
        _family.SaveSettings();
    }

    private async Task SelectPhotosAsync()
    {
        if (await CanShowPhotosAsync())
        {
            _family.Settings.SceneId = SceneCatalog.PhotosId;
            _family.Settings.UsesPhotos = true;
            _family.SaveSettings();
        }
    }

    public async Task SetPhotosBehindSceneAsync(bool isOn)
    {
        if (!CanTogglePhotos || (isOn && !await CanShowPhotosAsync()))
        {
            return;
        }

        _family.Settings.UsesPhotos = isOn;
        _family.SaveSettings();
    }

    private async Task<bool> CanShowPhotosAsync()
    {
        if (!_fullVersion.IsFull)
        {
            await OfferFullVersionAsync();
            return false;
        }

        if (_family.Settings.PhotoIds.Count > 0)
        {
            return true;
        }

        if (await _gate.PassAsync())
        {
            // Nothing to show yet, and adding photos is for a grown-up.
            _navigation.Navigate<ParentZoneViewModel>();
        }

        return false;
    }

    // Says why first, so a locked tap doesn't just land a kid on a maths question.
    private async Task OfferFullVersionAsync()
    {
        if (await _messages.ConfirmAsync(_localizer["FullVersionLockedTitle"], _localizer["FullVersionPrompt"])
            && await _gate.PassAsync())
        {
            _navigation.Navigate<ParentZoneViewModel>();
        }
    }

    [RelayCommand]
    private void EnterFamilyDisplay() => _display.Enter();

    [RelayCommand]
    private void EnterCompactOverlay() => _overlay.Enter();

    private void OnOverlayChanged(object? sender, EventArgs e) => IsScenePaused = _overlay.IsActive;

    private void OnSecond(object? sender, EventArgs e) => Refresh();

    // Under the overlay the scene is paused, and the overlay's own scene answers instead.
    private void OnShaken(object? sender, ShakeEventArgs e)
    {
        if (!IsScenePaused)
        {
            Shake = e;
        }
    }

    private void OnFamilyChanged(object? sender, EventArgs e) => Reload();

    private void Reload()
    {
        var settings = _family.Settings;
        SceneId = SceneCatalog.Resolve(settings.SceneId, _fullVersion.IsFull).Id;

        // With no photo to show, the Photos scene falls back to a painted one, and the list says so.
        var isPhotoScene = Backdrop.IsPhotoScene && Backdrop.IsActive;
        CanTogglePhotos = !isPhotoScene;
        PhotosBehindScene = Backdrop.IsActive;
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
            Scenes.Add(new SceneOptionViewModel(scene.Id, _localizer[$"Scene_{scene.Id}"], scene.RequiresFullVersion && !_fullVersion.IsFull, !isPhotoScene && scene.Id == SceneId));
        }

        Scenes.Add(new SceneOptionViewModel(PhotosOptionId, _localizer["Scene_photos"], !_fullVersion.IsFull, isPhotoScene));

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
