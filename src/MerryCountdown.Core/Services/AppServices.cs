namespace MerryCountdown.Core.Services;

public interface IAppClock
{
    DateTime Now { get; }
}

/// <summary>One UI-thread heartbeat per second, shared by everything that shows time.</summary>
public interface ITicker
{
    event EventHandler? Second;
}

/// <summary>The full-screen Family Display for a TV or tablet left running all advent.</summary>
public interface IDisplayModeService
{
    bool IsActive { get; }

    event EventHandler? Changed;

    void Enter();

    void Exit();
}

/// <summary>The countdown in a small always-on-top window, while the PC is used for other things.</summary>
public interface ICompactOverlayService
{
    /// <summary>False on heads with no overlay window, where the option is hidden.</summary>
    bool IsSupported { get; }

    bool IsActive { get; }

    event EventHandler? Changed;

    void Enter();

    void Exit();
}

public interface ILauncherService
{
    Task<bool> LaunchUriAsync(Uri uri);
}

public interface IShareService
{
    Task ShareAsync(string title, string uri);

    Task ShareTextAsync(string title, string text);
}

public interface ISnackService
{
    Task ShowMessageAsync(string title, string message);

    Task<bool> ConfirmAsync(string title, string message);
}

/// <param name="Added">Ids of the photos now stored. Empty when the picker was cancelled.</param>
/// <param name="Failed">How many picked files couldn't be read as pictures.</param>
public sealed record PhotoPickResult(IReadOnlyList<string> Added, int Failed);

/// <summary>Family photos, kept in the app's own storage so they never depend on the original file.</summary>
public interface IPhotoLibrary
{
    Task<PhotoPickResult> PickAndStoreAsync();

    /// <summary>Null when the photo is gone, for example after the browser cleared its storage.</summary>
    byte[]? Load(string id);

    void Delete(string id);
}
