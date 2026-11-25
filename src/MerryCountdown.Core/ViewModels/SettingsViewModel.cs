using MerryCountdown.Core.Family;
using MerryCountdown.Core.Infrastructure;
using MerryCountdown.Core.Parents;
using MerryCountdown.Core.Season;
using MerryCountdown.Core.Services.Settings;
using MerryCountdown.Services.Settings;
using MerryCountdown.Services.Theming;

namespace MerryCountdown.Core.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IStringLocalizer _localizer;
    private readonly IAppPreferences _appPreferences;
    private readonly IThemeManager _themeManager;
    private readonly IPreferences _preferences;
    private readonly IApplication _application;
    private readonly IFamilyService _family;
    private readonly IParentGateService _gate;
    private readonly INavigationService _navigation;
    private readonly IDisplayModeService _display;
    private readonly ILauncherService _launcher;
    private readonly IScreenWake _screenWake;
    private bool _isInitializing;

    public SettingsViewModel(
        IStringLocalizer localizer,
        IAppPreferences appPreferences,
        IThemeManager themeManager,
        IPreferences preferences,
        IApplication application,
        IFamilyService family,
        IParentGateService gate,
        INavigationService navigation,
        IDisplayModeService display,
        ILauncherService launcher,
        IScreenWake screenWake)
    {
        _localizer = localizer;
        _appPreferences = appPreferences;
        _themeManager = themeManager;
        _preferences = preferences;
        _application = application;
        _family = family;
        _gate = gate;
        _navigation = navigation;
        _display = display;
        _launcher = launcher;
        _screenWake = screenWake;
        PageTitle = _localizer["Settings"];
    }

    public override void OnNavigatedTo(object? parameter)
    {
        base.OnNavigatedTo(parameter);
        try
        {
            _isInitializing = true;
            Theme = _appPreferences.Theme;
            KeepScreenOn = _screenWake.KeepScreenOn;
            var settings = _family.Settings;
            SnowIndex = (int)settings.Snow;
            CalmMotion = settings.CalmMotion;
            SoundEnabled = settings.SoundEnabled;
            DisplayRotatesScenes = settings.DisplayRotatesScenes;
            StartInFamilyDisplay = settings.StartInFamilyDisplay;
        }
        finally
        {
            _isInitializing = false;
        }
    }

    public ElementTheme[] ThemeOptions { get; } = [ElementTheme.Default, ElementTheme.Light, ElementTheme.Dark];

    public string[] SnowOptions => [_localizer["Snow_Off"], _localizer["Snow_Gentle"], _localizer["Snow_Blizzard"]];

    [ObservableProperty]
    public partial ElementTheme Theme { get; set; }

    [ObservableProperty]
    public partial int SnowIndex { get; set; }

    [ObservableProperty]
    public partial bool CalmMotion { get; set; }

    [ObservableProperty]
    public partial bool SoundEnabled { get; set; }

    [ObservableProperty]
    public partial bool DisplayRotatesScenes { get; set; }

    [ObservableProperty]
    public partial bool StartInFamilyDisplay { get; set; }

    [ObservableProperty]
    public partial bool KeepScreenOn { get; set; }

    partial void OnKeepScreenOnChanged(bool value)
    {
        if (!_isInitializing)
        {
            _screenWake.KeepScreenOn = value;
        }
    }

    partial void OnThemeChanged(ElementTheme value)
    {
        if (_isInitializing)
        {
            return;
        }

        _themeManager.SetTheme(value);
        _appPreferences.Theme = value;
    }

    partial void OnSnowIndexChanged(int value)
    {
        if (value >= 0)
        {
            UpdateSettings(s => s.Snow = (SnowLevel)value);
        }
    }

    partial void OnCalmMotionChanged(bool value) => UpdateSettings(s => s.CalmMotion = value);

    partial void OnSoundEnabledChanged(bool value) => UpdateSettings(s => s.SoundEnabled = value);

    partial void OnDisplayRotatesScenesChanged(bool value) => UpdateSettings(s => s.DisplayRotatesScenes = value);

    partial void OnStartInFamilyDisplayChanged(bool value) => UpdateSettings(s => s.StartInFamilyDisplay = value);

    public string AppVersion => _application.AppVersion;

    /// <summary>
    /// Localized "Worktree: {name}" line shown under the version, or <see langword="null"/> when
    /// this build did not come from a git worktree. Composed here rather than in XAML because
    /// {markup:Localize} takes no format arguments.
    /// </summary>
    public string? WorktreeLabel =>
        _application.WorktreeName is { Length: > 0 } worktree
            ? _localizer["WorktreeFormat", worktree].Value
            : null;

    public bool IsDebug =>
#if DEBUG
        true;
#else
        false;
#endif

    [RelayCommand]
    private void ClearPreferences()
    {
        _preferences.Clear();
    }

    [RelayCommand]
    private void EnterFamilyDisplay() => _display.Enter();

    [RelayCommand]
    private async Task OpenParentZoneAsync()
    {
        if (await _gate.PassAsync())
        {
            _navigation.Navigate<ParentZoneViewModel>();
        }
    }

    private void UpdateSettings(Action<FamilySettings> change)
    {
        if (_isInitializing)
        {
            return;
        }

        change(_family.Settings);
        _family.SaveSettings();
    }
}
