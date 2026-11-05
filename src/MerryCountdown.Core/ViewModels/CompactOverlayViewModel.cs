using MerryCountdown.Core.Family;
using MerryCountdown.Core.Infrastructure;
using MerryCountdown.Core.Season;
using MerryCountdown.Core.Store;

namespace MerryCountdown.Core.ViewModels;

/// <summary>Just the sign on the family's scene: all that fits in a little window floating over other apps.</summary>
public partial class CompactOverlayViewModel : ViewModelBase
{
    private readonly IFamilyService _family;
    private readonly IAppClock _clock;
    private readonly ITicker _ticker;
    private readonly ICompactOverlayService _overlay;
    private readonly ISoundService _sounds;

    public CompactOverlayViewModel(
        IFamilyService family,
        IFullVersionService fullVersion,
        IAppClock clock,
        ITicker ticker,
        IStringLocalizer localizer,
        ICompactOverlayService overlay,
        PhotoBackdrop backdrop,
        ISoundService sounds)
    {
        _family = family;
        _clock = clock;
        _ticker = ticker;
        _overlay = overlay;
        _sounds = sounds;
        Backdrop = backdrop;
        Sign = new CountdownSignViewModel(localizer);
        SceneId = backdrop.SceneFor(SceneCatalog.Resolve(family.Settings.SceneId, fullVersion.IsFull).Id);
        Snow = family.Settings.Snow;
        CalmMotion = family.Settings.CalmMotion;
        Tick();
    }

    public PhotoBackdrop Backdrop { get; }

    public CountdownSignViewModel Sign { get; }

    public string SceneId { get; }

    public SnowLevel Snow { get; }

    public bool CalmMotion { get; }

    public void Start() => _ticker.Second += OnSecond;

    public void Stop() => _ticker.Second -= OnSecond;

    public void SceneTapped() => _sounds.Play(SoundEffect.SceneTap);

    [RelayCommand]
    private void Exit() => _overlay.Exit();

    private void OnSecond(object? sender, EventArgs e) => Tick();

    private void Tick() => Sign.Update(_clock.Now, _family.Settings);
}
