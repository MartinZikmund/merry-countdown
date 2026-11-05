using MerryCountdown.Core.Family;
using MerryCountdown.Core.Parents;
using MerryCountdown.Core.Store;
using MerryCountdown.Services.Dialogs;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Windows.Storage;

namespace MerryCountdown.Services;

/// <summary>December is untestable for eleven months of the year, so debug builds can pretend.</summary>
public sealed class SystemClock : IAppClock
{
    private readonly TimeSpan _offset = FindOffset();

    public DateTime Now => DateTime.Now + _offset;

    private static TimeSpan FindOffset()
    {
#if DEBUG
        // MERRY_FAKE_DATE, or a devdate.txt in LocalFolder for packaged apps that don't inherit the environment.
        var text = Environment.GetEnvironmentVariable("MERRY_FAKE_DATE");
        if (string.IsNullOrEmpty(text))
        {
            var path = Path.Combine(ApplicationData.Current.LocalFolder.Path, "devdate.txt");
            text = File.Exists(path) ? File.ReadAllText(path).Trim() : null;
        }

        if (DateTime.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out var fake))
        {
            return fake - DateTime.Now;
        }
#endif
        return TimeSpan.Zero;
    }
}

public sealed class UiTicker : ITicker
{
    private readonly DispatcherQueueTimer _timer;
    private EventHandler? _second;

    public UiTicker()
    {
        _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (_, _) => _second?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? Second
    {
        add
        {
            _second += value;
            _timer.Start();
        }

        remove
        {
            _second -= value;
            if (_second is null)
            {
                _timer.Stop();
            }
        }
    }
}

public sealed class FamilyEntitlementCache(FamilyService family) : IEntitlementCache
{
    public bool IsFull
    {
        get => family.IsFullVersionCached;
        set => family.IsFullVersionCached = value;
    }
}

/// <summary>Full screen, screen kept on, no chrome: what a TV or tablet needs to run all advent.</summary>
public sealed class DisplayModeService(IWindowShellProvider shells, IDisplayRequestManager displayRequests) : IDisplayModeService
{
    private IDisposable? _keepAwake;

    public bool IsActive { get; private set; }

    public event EventHandler? Changed;

    public void Enter()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        SetFullScreen(true);
        KeepAwake(true);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Exit()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        SetFullScreen(false);
        KeepAwake(false);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void KeepAwake(bool stayAwake)
    {
        _keepAwake?.Dispose();
        _keepAwake = stayAwake ? displayRequests.RequestActive() : null;

#if !WINAPPSDK
        // DisplayRequest is a no-op on Skia desktop, but a PC plugged into a TV still has to stay on.
        if (OperatingSystem.IsWindows())
        {
            const uint Continuous = 0x80000000;
            const uint SystemRequired = 0x00000001;
            const uint DisplayRequired = 0x00000002;
            SetThreadExecutionState(stayAwake ? Continuous | SystemRequired | DisplayRequired : Continuous);
        }
#endif
    }

#if !WINAPPSDK
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern uint SetThreadExecutionState(uint flags);
#endif

    private void SetFullScreen(bool isFullScreen)
    {
        try
        {
            shells.Window.AppWindow.SetPresenter(isFullScreen ? AppWindowPresenterKind.FullScreen : AppWindowPresenterKind.Default);
        }
        catch (Exception exception) when (exception is NotSupportedException or NotImplementedException or InvalidOperationException)
        {
            // Some browsers refuse without a user gesture, e.g. on a --display launch. The display still works, just windowed.
        }
    }
}

public sealed class CompactOverlayService(IWindowShellProvider shells, IXamlRootProvider xamlRoots) : ICompactOverlayService
{
    // Windows sizes the overlay from the work area, which on an ultrawide means a very wide "small" window.
    private const int WidthInDips = 420;
    private const int HeightInDips = 236;

    private AppWindowPresenter? _previous;
    private Windows.UI.Color? _previousCaptionColor;

    // Uno's AppWindow throws NotSupportedException for CompactOverlay on every non-Windows target.
#if WINAPPSDK
    public bool IsSupported => true;
#else
    public bool IsSupported => false;
#endif

    public bool IsActive { get; private set; }

    public event EventHandler? Changed;

    public void Enter()
    {
        if (IsActive || !IsSupported)
        {
            return;
        }

        var window = shells.Window.AppWindow;
        _previous = window.Presenter;
        window.SetPresenter(AppWindowPresenterKind.CompactOverlay);

        var scale = xamlRoots.XamlRoot.RasterizationScale;
        window.ResizeClient(new Windows.Graphics.SizeInt32((int)(WidthInDips * scale), (int)(HeightInDips * scale)));

        // The caption buttons sit on the scene here, not on the themed page, so the theme's colour may vanish into it.
        _previousCaptionColor = window.TitleBar.ButtonForegroundColor;
        window.TitleBar.ButtonForegroundColor = Microsoft.UI.Colors.White;

        IsActive = true;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Exit()
    {
        if (!IsActive)
        {
            return;
        }

        // The same presenter, not a new default one, so a maximized window comes back maximized.
        var window = shells.Window.AppWindow;
        window.SetPresenter(_previous);
        window.TitleBar.ButtonForegroundColor = _previousCaptionColor;
        _previous = null;
        IsActive = false;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}

public sealed class SnackService(IDialogService dialogs, IConfirmationDialogService confirmations) : ISnackService
{
    public async Task ShowMessageAsync(string title, string message) => await dialogs.ShowAsync(title, message);

    public async Task<bool> ConfirmAsync(string title, string message) =>
        await confirmations.ShowAsync(title, message) == ConfirmationResult.Confirmed;
}

/// <summary>Multiplication in words: a parent solves it in a second, a five-year-old doesn't.</summary>
public sealed class ParentGateService(IDialogService dialogs, IStringLocalizer localizer) : IParentGateService
{
    private readonly ParentGate _gate = new(Random.Shared);

    public async Task<bool> PassAsync()
    {
        var challenge = _gate.Next();
        TextBox answer = new()
        {
            PlaceholderText = localizer["ParentGatePlaceholder"],
            InputScope = new Microsoft.UI.Xaml.Input.InputScope { Names = { new Microsoft.UI.Xaml.Input.InputScopeName(Microsoft.UI.Xaml.Input.InputScopeNameValue.Number) } },
        };
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetAutomationId(answer, "ParentGateAnswer");
        Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(answer, localizer["ParentGatePlaceholder"]);

        StackPanel content = new() { Spacing = 12 };
        content.Children.Add(new TextBlock { Text = localizer["ParentGateMessage"], TextWrapping = TextWrapping.Wrap });
        content.Children.Add(new TextBlock
        {
            Text = localizer["ParentGateQuestion", localizer[$"Number_{challenge.Left}"], localizer[$"Number_{challenge.Right}"]],
            Style = (Style)Application.Current.Resources["BodyStrongTextBlockStyle"],
            TextWrapping = TextWrapping.Wrap,
        });
        content.Children.Add(answer);

        ContentDialog dialog = new()
        {
            Title = localizer["ParentGateTitle"],
            Content = content,
            PrimaryButtonText = localizer["ParentGateContinue"],
            CloseButtonText = localizer["ParentGateCancel"],
            DefaultButton = ContentDialogButton.Primary,
        };

        return await dialogs.ShowAsync(dialog) == ContentDialogResult.Primary
            && ParentGate.Check(challenge, answer.Text);
    }
}
