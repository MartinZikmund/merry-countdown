using MerryCountdown.Core.Family;
using MerryCountdown.Core.Store;

namespace MerryCountdown.Core.ViewModels;

/// <summary>The family photo slideshow behind the countdown. One per window, so pages share its place in the album.</summary>
public partial class PhotoBackdrop : ObservableObject
{
    private readonly IFamilyService _family;
    private readonly IFullVersionService _fullVersion;
    private readonly IPhotoLibrary _library;
    private string? _currentId;
    private int _secondsOnPhoto;

    public PhotoBackdrop(IFamilyService family, IFullVersionService fullVersion, IPhotoLibrary library, ITicker ticker)
    {
        _family = family;
        _fullVersion = fullVersion;
        _library = library;

        family.Changed += (_, _) => Reload();
        fullVersion.Changed += (_, _) => Reload();
        ticker.Second += (_, _) => OnSecond();
        Reload();
    }

    [ObservableProperty]
    public partial byte[]? Photo { get; set; }

    [ObservableProperty]
    public partial bool ShowForeground { get; set; } = true;

    public bool IsActive => Photo is not null;

    /// <summary>Which scene to draw: its foreground goes in front of the photo, or the whole scene without one.</summary>
    public string SceneFor(string chosenScene) =>
        IsActive ? _family.Settings.PhotoForegroundSceneId ?? chosenScene : chosenScene;

    public void Next()
    {
        _secondsOnPhoto = 0;
        var ids = _family.Settings.PhotoIds;
        if (!IsWanted || ids.Count < 2)
        {
            return;
        }

        var start = ids.IndexOf(_currentId ?? "");
        for (var step = 1; step <= ids.Count; step++)
        {
            if (TryShow(ids[(start + step) % ids.Count]))
            {
                return;
            }
        }
    }

    private bool IsWanted => _family.Settings.UsesPhotos && _fullVersion.IsFull;

    private void OnSecond()
    {
        if (IsActive && ++_secondsOnPhoto >= _family.Settings.PhotoIntervalSeconds)
        {
            Next();
        }
    }

    private void Reload()
    {
        var settings = _family.Settings;
        var stillThere = IsWanted && Photo is not null && _currentId is not null && settings.PhotoIds.Contains(_currentId);
        if (!stillThere && !(IsWanted && settings.PhotoIds.Any(TryShow)))
        {
            _currentId = null;
            Photo = null;
        }

        ShowForeground = !IsActive || settings.PhotoForegroundSceneId is not null;
        OnPropertyChanged(nameof(IsActive));
    }

    private bool TryShow(string id)
    {
        if (_library.Load(id) is not { } bytes)
        {
            return false;
        }

        _currentId = id;
        Photo = bytes;
        return true;
    }
}
