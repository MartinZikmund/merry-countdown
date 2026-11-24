using MerryCountdown.Core.Family;
using MerryCountdown.Core.Tests.Fakes;
using MerryCountdown.Core.ViewModels;
using FluentAssertions;

namespace MerryCountdown.Core.Tests.ViewModels;

[TestClass]
public class SettingsViewModelTests
{
    private const string WorktreeFormatKey = "WorktreeFormat";

    [TestMethod]
    public void WorktreeLabel_WhenNotInWorktree_ReturnsNull()
    {
        var viewModel = CreateViewModel(worktreeName: null);

        viewModel.WorktreeLabel.Should().BeNull();
    }

    [TestMethod]
    public void WorktreeLabel_WhenWorktreeNameIsEmpty_ReturnsNull()
    {
        var viewModel = CreateViewModel(worktreeName: string.Empty);

        viewModel.WorktreeLabel.Should().BeNull();
    }

    [TestMethod]
    public void WorktreeLabel_WhenInWorktree_IncludesTheWorktreeName()
    {
        var viewModel = CreateViewModel(
            worktreeName: "identity",
            strings: new Dictionary<string, string> { [WorktreeFormatKey] = "Worktree: {0}" });

        viewModel.WorktreeLabel.Should().Be("Worktree: identity");
    }

    [TestMethod]
    public void WorktreeLabel_WhenInWorktree_UsesTheLocalizedFormatString()
    {
        var viewModel = CreateViewModel(
            worktreeName: "identity",
            strings: new Dictionary<string, string> { [WorktreeFormatKey] = "Pracovni strom: {0}" });

        viewModel.WorktreeLabel.Should().Be("Pracovni strom: identity");
    }

    [TestMethod]
    public void KeepScreenOn_WhenToggled_IsPassedToScreenWake()
    {
        FakeScreenWake wake = new();
        var viewModel = CreateViewModel(worktreeName: null, screenWake: wake);
        viewModel.OnNavigatedTo(null);

        viewModel.KeepScreenOn = true;

        wake.KeepScreenOn.Should().BeTrue();
    }

    [TestMethod]
    public void OnNavigatedTo_ShowsTheSavedKeepScreenOn()
    {
        var viewModel = CreateViewModel(worktreeName: null, screenWake: new FakeScreenWake { KeepScreenOn = true });

        viewModel.OnNavigatedTo(null);

        viewModel.KeepScreenOn.Should().BeTrue();
    }

    private static SettingsViewModel CreateViewModel(
        string? worktreeName,
        IDictionary<string, string>? strings = null,
        FakeScreenWake? screenWake = null) =>
        new(
            new FakeStringLocalizer(strings),
            new FakeAppPreferences(),
            new FakeThemeManager(),
            new FakePreferences(),
            new FakeApplication { WorktreeName = worktreeName },
            new FamilyService(new FakeFamilyStore(), "en-US"),
            new FakeGate(),
            new FakeNavigation(),
            new FakeDisplayMode(),
            new FakeLauncher(),
            screenWake ?? new FakeScreenWake());
}
