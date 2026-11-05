using MerryCountdown.Core.Family;
using MerryCountdown.Core.Navigation;
using MerryCountdown.Core.Parents;
using MerryCountdown.Core.Services;
using MerryCountdown.Core.Store;

namespace MerryCountdown.Core.Tests.Fakes;

internal sealed class FakeClock(DateTime now) : IAppClock
{
    public DateTime Now { get; set; } = now;
}

internal sealed class FakeTicker : ITicker
{
    public event EventHandler? Second;

    public void Fire() => Second?.Invoke(this, EventArgs.Empty);
}

internal sealed class FakeFamilyStore : IFamilyStore
{
    public FamilyData Data { get; set; } = new();

    public FamilyData Load() => Data;

    public void Save(FamilyData data) => Data = data;
}

internal sealed class FakeFullVersion(bool isFull = false) : IFullVersionService
{
    public bool IsFull { get; set; } = isFull;

    public bool CanPurchase => !IsFull;

    public event EventHandler? Changed;

    public Task<string?> GetPriceAsync() => Task.FromResult<string?>("€3.99");

    public Task RefreshAsync(CancellationToken ct = default) => Task.CompletedTask;

    public void Revoke()
    {
        IsFull = false;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public Task<PurchaseResult> PurchaseAsync(CancellationToken ct = default)
    {
        IsFull = true;
        Changed?.Invoke(this, EventArgs.Empty);
        return Task.FromResult(PurchaseResult.Succeeded);
    }
}

internal sealed class FakeNavigation : INavigationService
{
    public List<Type> Visited { get; } = [];

    public bool CanGoBack => false;

    public NavigationSection? CurrentSection => null;

    public void Initialize()
    {
    }

    public void Navigate<TViewModel>() => Visited.Add(typeof(TViewModel));

    public void Navigate<TViewModel>(object? parameter) => Visited.Add(typeof(TViewModel));

    public bool GoBack() => false;

    public void ClearBackStack()
    {
    }
}

internal sealed class FakeDisplayMode : IDisplayModeService
{
    public bool IsActive { get; private set; }

    public event EventHandler? Changed;

    public void Enter()
    {
        IsActive = true;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Exit()
    {
        IsActive = false;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}

internal sealed class FakeGate(bool passes = true) : IParentGateService
{
    public int Asked { get; private set; }

    public bool Passes { get; set; } = passes;

    public Task<bool> PassAsync()
    {
        Asked++;
        return Task.FromResult(Passes);
    }
}

internal sealed class FakeLauncher : ILauncherService
{
    public List<Uri> Launched { get; } = [];

    public Task<bool> LaunchUriAsync(Uri uri)
    {
        Launched.Add(uri);
        return Task.FromResult(true);
    }
}

internal sealed class FakeShare : IShareService
{
    public string? LastText { get; private set; }

    public Task ShareAsync(string title, string uri) => Task.CompletedTask;

    public Task ShareTextAsync(string title, string text)
    {
        LastText = text;
        return Task.CompletedTask;
    }
}

internal sealed class FakeMessages : ISnackService
{
    public int Shown { get; private set; }

    public bool ConfirmResult { get; set; } = true;

    public Task ShowMessageAsync(string title, string message)
    {
        Shown++;
        return Task.CompletedTask;
    }

    public Task<bool> ConfirmAsync(string title, string message) => Task.FromResult(ConfirmResult);
}
