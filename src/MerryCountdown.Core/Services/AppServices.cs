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
