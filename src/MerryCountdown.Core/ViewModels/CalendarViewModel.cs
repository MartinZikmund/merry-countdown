using System.Collections.ObjectModel;
using System.Globalization;
using MerryCountdown.Core.Content;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Infrastructure;
using MerryCountdown.Core.Parents;
using MerryCountdown.Core.Season;

namespace MerryCountdown.Core.ViewModels;

public partial class CalendarViewModel : ViewModelBase
{
    private readonly IFamilyService _family;
    private readonly IAppClock _clock;
    private readonly ITicker _ticker;
    private readonly IStringLocalizer _localizer;
    private readonly IParentGateService _gate;
    private readonly ILauncherService _launcher;
    private readonly ISoundService _sounds;
    private readonly DoorLibrary _library;
    private DateTime _builtFor;
    private Kid? _builtForKid;

    public CalendarViewModel(
        IFamilyService family,
        IAppClock clock,
        ITicker ticker,
        IStringLocalizer localizer,
        IParentGateService gate,
        ILauncherService launcher,
        KidPickerViewModel kidPicker,
        ISoundService sounds)
    {
        _family = family;
        _clock = clock;
        _ticker = ticker;
        _localizer = localizer;
        _gate = gate;
        _launcher = launcher;
        _sounds = sounds;
        _library = DoorLibrary.Load(CultureInfo.CurrentUICulture.Name);
        KidPicker = kidPicker;
        PageTitle = localizer["NavCalendar"];
        RebuildDoors();
    }

    public KidPickerViewModel KidPicker { get; }

    public ObservableCollection<DoorViewModel> Doors { get; } = [];

    [ObservableProperty]
    public partial string Hint { get; set; } = "";

    [ObservableProperty]
    public partial string Progress { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRevealOpen))]
    public partial DoorRevealViewModel? Reveal { get; set; }

    public bool IsRevealOpen => Reveal is not null;

    /// <summary>Arriving with a door number means someone just opened that door on the home screen.</summary>
    public override void ViewLoaded()
    {
        _ticker.Second += OnSecond;
        _family.Changed += OnFamilyChanged;
        RebuildDoors();
    }

    public override void ViewUnloaded()
    {
        _ticker.Second -= OnSecond;
        _family.Changed -= OnFamilyChanged;
    }

    public void DoorStartedSwinging() => _sounds.Play(SoundEffect.DoorOpen);

    [RelayCommand]
    public void TapDoor(DoorViewModel door)
    {
        var now = _clock.Now;
        var year = _family.Settings.Advent(now).Year;
        var kid = _family.ActiveKid;

        switch (door.State)
        {
            case DoorState.Locked:
                Hint = _localizer.Plural("DoorLockedHint", _family.Settings.Advent(now).SleepsUntil(door.Number, now));
                door.Jiggle();
                _sounds.Play(SoundEffect.DoorLocked);
                break;
            case DoorState.Ready:
                // A swinging door has already creaked. With Calm motion it doesn't swing, so it creaks now.
                if (!door.ShouldSwingOpen)
                {
                    _sounds.Play(SoundEffect.DoorOpen);
                }

                _family.OpenDoor(kid, year, door.Number);
                ShowReveal(door, year, kid);
                break;
            case DoorState.SneakPeek:
                // Not marked as opened, so its real day still gets the full moment.
                _sounds.Play(SoundEffect.Reveal);
                ShowReveal(door, year, kid, isSneakPeek: true);
                break;
            default:
                _sounds.Play(SoundEffect.Reveal);
                ShowReveal(door, year, kid);
                break;
        }
    }

    [RelayCommand]
    private void CloseReveal() => Reveal = null;

    [RelayCommand]
    private async Task OpenVideoAsync()
    {
        if (Reveal?.VideoUrl is { } url && await _gate.PassAsync())
        {
            await _launcher.LaunchUriAsync(new Uri(url));
        }
    }

    private void ShowReveal(DoorViewModel door, int year, Kid kid, bool isSneakPeek = false)
    {
        var content = _library.For(door.Number, year);
        var bringer = _localizer.BringerName(_family.Settings.GiftBringer);
        kid.DoorMessages.TryGetValue(door.Number, out var message);

        door.Refresh(_library, _clock.Now, year, _family, kid);
        UpdateProgress(year, kid);
        Hint = "";
        Reveal = new DoorRevealViewModel(door.Number, content.Title, content.Body.Replace("{bringer}", bringer), content.Sticker, content.Punchline, content.VideoUrl, message, content.Kind == DoorKind.Finale, _sounds, isSneakPeek);
    }

    private void OnSecond(object? sender, EventArgs e)
    {
        if (_clock.Now.Date != _builtFor)
        {
            RebuildDoors();
        }
    }

    private void OnFamilyChanged(object? sender, EventArgs e)
    {
        if (_family.ActiveKid != _builtForKid)
        {
            Reveal = null;
            Hint = "";
            RebuildDoors();
        }
    }

    private void RebuildDoors()
    {
        var now = _clock.Now;
        var year = _family.Settings.Advent(now).Year;
        var kid = _family.ActiveKid;
        _builtFor = now.Date;
        _builtForKid = kid;

        if (Doors.Count == 0)
        {
            foreach (var number in AdventCalendar.LayoutFor(year))
            {
                Doors.Add(new DoorViewModel(number, _localizer));
            }
        }

        foreach (var door in Doors)
        {
            door.Refresh(_library, now, year, _family, kid);
        }

        UpdateProgress(year, kid);
    }

    private void UpdateProgress(int year, Kid kid)
    {
        var opened = Enumerable.Range(1, AdventCalendar.DoorCount).Count(d => _family.IsDoorOpened(kid, year, d));
        Progress = _localizer["CalendarProgress", opened, AdventCalendar.DoorCount];
    }
}

public partial class DoorViewModel(int number, IStringLocalizer localizer) : ObservableObject
{
    private const int TintCount = 5;

    public int Number { get; } = number;

    /// <summary>Doors come in a few colours so the wall looks hand-painted, not spreadsheet-generated.</summary>
    public int Tint { get; } = ((number * 7) + 3) % TintCount;

    public event EventHandler? Jiggled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOpened))]
    public partial DoorState State { get; set; }

    [ObservableProperty]
    public partial bool IsToday { get; set; }

    [ObservableProperty]
    public partial string Sticker { get; set; } = "star";

    [ObservableProperty]
    public partial string AccessibleName { get; set; } = "";

    public bool IsOpened => State == DoorState.Opened;

    [ObservableProperty]
    public partial bool CalmMotion { get; set; }

    public bool ShouldSwingOpen => State == DoorState.Ready && !CalmMotion;

    public void Jiggle() => Jiggled?.Invoke(this, EventArgs.Empty);

    internal void Refresh(DoorLibrary library, DateTime now, int year, IFamilyService family, Kid kid)
    {
        CalmMotion = family.Settings.CalmMotion;
        var season = family.Settings.Advent(now);
        State = season.StateOf(Number, now, family.IsDoorOpened(kid, year, Number));
        IsToday = season.TodaysDoor(now) == Number;
        Sticker = library.For(Number, year).Sticker;
        AccessibleName = localizer[$"DoorAccessible_{State}", Number];
    }
}

public partial class DoorRevealViewModel(int number, string title, string body, string sticker, string? punchline, string? videoUrl, string? personalMessage, bool isFinale, ISoundService sounds, bool isSneakPeek = false) : ObservableObject
{
    public int Number { get; } = number;

    public string Title { get; } = title;

    public string Body { get; } = body;

    public string Sticker { get; } = sticker;

    public string? Punchline { get; } = punchline;

    public string? VideoUrl { get; } = videoUrl;

    public string? PersonalMessage { get; } = personalMessage;

    public bool IsFinale { get; } = isFinale;

    /// <summary>Door 1 shown before advent starts. The card says so, and the door stays shut.</summary>
    public bool IsSneakPeek { get; } = isSneakPeek;

    public bool HasPersonalMessage => !string.IsNullOrEmpty(PersonalMessage);

    public bool IsVideo => VideoUrl is not null;

    public bool CanRevealPunchline => Punchline is not null && !IsPunchlineShown;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRevealPunchline))]
    public partial bool IsPunchlineShown { get; set; }

    [RelayCommand]
    private void ShowPunchline()
    {
        IsPunchlineShown = true;
        sounds.Play(SoundEffect.Punchline);
    }
}
