using MerryCountdown.Core.Navigation;
using MerryCountdown.Core.Services;
using MerryCountdown.Core.ViewModels;
using MerryCountdown.Infrastructure;
using MerryCountdown.Services.Navigation;
using MerryCountdown.Services.Settings;
using MerryCountdown.Services.Theming;
using Microsoft.UI.Windowing;
using Windows.Foundation.Metadata;

namespace MerryCountdown;

public sealed partial class WindowShell : Page, IWindowShell
{
    private readonly IServiceScope _windowScope;
    private readonly Window _associatedWindow;
    private bool _isWindowClosed;
    private ViewModelBase? _currentPageViewModel;
    private readonly IDisplayModeService _display;
    private readonly ICompactOverlayService _compactOverlay;
    private readonly IScreenWake _screenWake;
    private Controls.LookPicker? _lookPicker;
    private readonly WelcomeTips _welcomeTips;
    private bool _isWelcomeOffered;

    public WindowShell(IServiceProvider serviceProvider, Window associatedWindow)
    {
        InitializeComponent();

        _windowScope = serviceProvider.CreateScope();
        var windowShellProvider = (WindowShellProvider)ServiceProvider.GetRequiredService<IWindowShellProvider>();
        windowShellProvider.SetShell(this, associatedWindow);

        var navigationService = ServiceProvider.GetRequiredService<INavigationService>();
        navigationService.Initialize();

        // Restore saved theme
        var settings = ServiceProvider.GetRequiredService<IAppPreferences>();
        var themeManager = ServiceProvider.GetRequiredService<IThemeManager>();
        themeManager.SetTheme(settings.Theme);

        _associatedWindow = associatedWindow;
        _associatedWindow.Closed += OnWindowClosed;
        CustomizeWindow();

        ViewModel = ServiceProvider.GetRequiredService<WindowShellViewModel>();
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
        MakeRoomForDoorBadge();

        InnerFrame.Navigated += InnerFrame_Navigated;
        _display = ServiceProvider.GetRequiredService<IDisplayModeService>();
        _display.Changed += OnDisplayModeChanged;
        _compactOverlay = ServiceProvider.GetRequiredService<ICompactOverlayService>();
        _compactOverlay.Changed += OnCompactOverlayChanged;
        _screenWake = ServiceProvider.GetRequiredService<IScreenWake>();
        _welcomeTips = ServiceProvider.GetRequiredService<WelcomeTips>();
        _welcomeTips.Changed += (_, _) => ShowWelcomeTip();
        TrackWindowVisibility();
        Loading += WindowShell_Loading;

        UpdateWindowTitle();
    }

    public IServiceProvider ServiceProvider => _windowScope.ServiceProvider;

    /// <summary>For a TV that should wake straight into the big screen.</summary>
    public void EnterFamilyDisplayOnLoad()
    {
        if (IsLoaded)
        {
            _display.Enter();
        }
        else
        {
            Loaded += EnterOnce;
        }

        void EnterOnce(object sender, RoutedEventArgs e)
        {
            Loaded -= EnterOnce;
            _display.Enter();
        }
    }

    private void OnDisplayModeChanged(object? sender, EventArgs e)
    {
        if (_display.IsActive)
        {
            _welcomeTips.Finish();
            DisplayHost.Content = new Views.DisplayView(ServiceProvider.GetRequiredService<DisplayViewModel>());
            DisplayHost.Visibility = Visibility.Visible;
        }
        else
        {
            DisplayHost.Content = null;
            DisplayHost.Visibility = Visibility.Collapsed;
        }
    }

    private void OnCompactOverlayChanged(object? sender, EventArgs e)
    {
        var isActive = _compactOverlay.IsActive;
        if (isActive)
        {
            _welcomeTips.Finish();
        }

        CompactOverlayHost.Content = isActive ? new Views.CompactOverlayView(ServiceProvider.GetRequiredService<CompactOverlayViewModel>()) : null;
        CompactOverlayHost.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;

        // Collapsed as well as covered, so Tab can't wander into a page nobody can see.
        AppContent.Visibility = isActive ? Visibility.Collapsed : Visibility.Visible;

#if WINAPPSDK
        OverlayDragRegion.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
        _associatedWindow.SetTitleBar(isActive ? OverlayDragRegion : DragRegion);
#endif
    }

    public WindowShellViewModel ViewModel { get; }

    public Frame RootFrame => InnerFrame;

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WindowShellViewModel.Title))
        {
            UpdateWindowTitle();
        }
        else if (e.PropertyName == nameof(WindowShellViewModel.HasWaitingDoor))
        {
            MakeRoomForDoorBadge();
        }
    }

    private void UpdateWindowTitle()
    {
        if (ViewModel.Title is not null && !_isWindowClosed)
        {
            // Worktree builds carry their own package identity; showing the worktree here is what
            // makes two simultaneously-running builds tellable apart in the taskbar and Alt-Tab.
            _associatedWindow.Title = AppEnvironment.WorktreeName is { Length: > 0 } worktree
                ? $"{ViewModel.Title} — {worktree}"
                : ViewModel.Title;
        }
    }

    private void InnerFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        TrackPageViewModel(e.Content);

        // The very first page has no XamlRoot yet, so it only gets its view model while loading.
        if (_currentPageViewModel is null && e.Content is FrameworkElement page)
        {
            page.Loaded += OnPageLoadedLate;
        }

        ViewModel.NotifyCanGoBackChanged();
        UpdateTabSelection();
    }

    private void OnPageLoadedLate(object sender, RoutedEventArgs e)
    {
        ((FrameworkElement)sender).Loaded -= OnPageLoadedLate;
        if (ReferenceEquals(InnerFrame.Content, sender))
        {
            TrackPageViewModel(sender);
        }
    }

    private void TrackPageViewModel(object? page)
    {
        if (_currentPageViewModel is not null)
        {
            _currentPageViewModel.PropertyChanged -= PageViewModel_PropertyChanged;
            _currentPageViewModel = null;
        }

        if (page is FrameworkElement { DataContext: ViewModelBase pageViewModel })
        {
            _currentPageViewModel = pageViewModel;
            _currentPageViewModel.PropertyChanged += PageViewModel_PropertyChanged;
            UpdatePageTitle();
        }
        else
        {
            ViewModel.Title = ServiceProvider.GetRequiredService<IStringLocalizer>()["ApplicationName"];
        }

        OfferCustomizing(_currentPageViewModel as CountdownViewModel);
    }

    // Only Home has a look to change. Each visit gets its own view model, so the picker is rebuilt with it.
    private void OfferCustomizing(CountdownViewModel? home)
    {
        if (_lookPicker is not null)
        {
            _lookPicker.CloseRequested -= OnLookPickerCloseRequested;
        }

        _lookPicker = home is null ? null : new Controls.LookPicker(home);
        if (_lookPicker is not null)
        {
            _lookPicker.CloseRequested += OnLookPickerCloseRequested;
        }

        CustomizeFlyout.Hide();
        CustomizeFlyout.Content = _lookPicker;
        CustomizeButton.Visibility = home is null ? Visibility.Collapsed : Visibility.Visible;

        if (home is not null)
        {
            OfferWelcomeTips();
        }
        else if (_welcomeTips.Current == WelcomeTip.Customize)
        {
            // The button it points at has just gone.
            _welcomeTips.Next();
        }
    }

    private bool IsBarInView => !_isWindowClosed && !_display.IsActive && !_compactOverlay.IsActive;

    private async void OfferWelcomeTips()
    {
        if (_isWelcomeOffered)
        {
            return;
        }

        // A moment for the scene to be seen first.
        _isWelcomeOffered = true;
        await Task.Delay(TimeSpan.FromSeconds(1.5));

        if (IsBarInView && CustomizeButton.Visibility == Visibility.Visible)
        {
            _welcomeTips.Begin();
        }
        else
        {
            _isWelcomeOffered = false;
        }
    }

    private void ShowWelcomeTip()
    {
        CustomizeTip.IsOpen = _welcomeTips.Current == WelcomeTip.Customize;
        SettingsTip.IsOpen = _welcomeTips.Current == WelcomeTip.Settings;
    }

    private void WelcomeTip_Next(TeachingTip sender, object args) => _welcomeTips.Next();

    private void WelcomeTip_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
    {
        if (args.Reason != TeachingTipCloseReason.Programmatic)
        {
            _welcomeTips.Finish();
        }
    }

    private void OnLookPickerCloseRequested(object? sender, EventArgs e) => CustomizeFlyout.Hide();

    private void PageViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModelBase.PageTitle))
        {
            UpdatePageTitle();
        }
    }

    private void UpdatePageTitle()
    {
        if (_currentPageViewModel is not null)
        {
            ViewModel.Title = _currentPageViewModel.PageTitle
                ?? ServiceProvider.GetRequiredService<IStringLocalizer>()["ApplicationName"];
        }
    }

    // Minimized, hidden or sent to the background: nobody is looking, so the screen may sleep.
    private void TrackWindowVisibility()
    {
        _associatedWindow.VisibilityChanged += (_, e) => _screenWake.SetWindowVisible(e.Visible);

#if WINAPPSDK
        // WinUI doesn't raise VisibilityChanged for minimize.
        _associatedWindow.AppWindow.Changed += (_, _) => _associatedWindow.DispatcherQueue.TryEnqueue(() =>
        {
            if (!_isWindowClosed)
            {
                var minimized = _associatedWindow.AppWindow.Presenter is OverlappedPresenter { State: OverlappedPresenterState.Minimized };
                _screenWake.SetWindowVisible(_associatedWindow.AppWindow.IsVisible && !minimized);
            }
        });
#endif
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        _isWindowClosed = true;
        _screenWake.SetWindowVisible(false);
        _associatedWindow.Closed -= OnWindowClosed;
        InnerFrame.Navigated -= InnerFrame_Navigated;
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
        if (_currentPageViewModel is not null)
        {
            _currentPageViewModel.PropertyChanged -= PageViewModel_PropertyChanged;
            _currentPageViewModel = null;
        }
    }

    private void WindowShell_Loading(FrameworkElement sender, object args)
    {
        var windowShellProvider = (WindowShellProvider)ServiceProvider.GetRequiredService<IWindowShellProvider>();
        windowShellProvider.SetXamlRoot(XamlRoot ?? throw new InvalidOperationException("XamlRoot must be set."));

        // A move to a monitor with another scale changes the caption buttons' size in DIPs.
        XamlRoot.Changed += (_, _) => ArrangeBars();
    }

    public void SetTitleBar(UIElement? titleBar)
    {
        if (_isWindowClosed)
        {
            return;
        }

#if WINAPPSDK
        _associatedWindow.SetTitleBar(titleBar ?? DragRegion);
#else
        _associatedWindow.SetTitleBar(titleBar);
#endif
    }

    private void CustomizeWindow()
    {
#if WINAPPSDK
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            _associatedWindow.ExtendsContentIntoTitleBar = true;
            _associatedWindow.SetTitleBar(DragRegion);
            _associatedWindow.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            _extendsIntoTitleBar = true;
        }
#endif

        if (ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.Window", "SystemBackdrop"))
        {
            _associatedWindow.SystemBackdrop = new MicaBackdrop();
            Background = null;
        }
    }

    #region Navigation

    private void AppContent_Loaded(object sender, RoutedEventArgs e)
        => NavigateToSection(NavigationSection.Countdown);

    private void AppContent_SizeChanged(object sender, SizeChangedEventArgs e) => ArrangeBars();

    private void AppBarContent_SizeChanged(object sender, SizeChangedEventArgs e) => ArrangeBars();

    // The badge hangs off the door's corner. Beside the label that's where the label starts, so the label moves over.
    private void MakeRoomForDoorBadge()
    {
        const double BadgeOverhang = 12;
        var isBesideLabel = CalendarTabContent.Orientation == Orientation.Horizontal;
        DoorSticker.Margin = new Thickness(0, 0, isBesideLabel && ViewModel.HasWaitingDoor ? BadgeOverhang : 0, 0);
    }

    private const double AppBarColumnGaps = 28;
    private const int TabsColumn = 2;

    private bool _isAtBottom;
    private double _tabsWidthOnTop;

    // The tabs in the top bar while they fit there, otherwise in a bar of their own at the bottom.
    private void ArrangeBars()
    {
        if (XamlRoot is null || AppContent.ActualWidth == 0)
        {
            return;
        }

        // Down in the bottom bar the tabs are a different width, so the last one they had up here is what counts.
        if (!_isAtBottom && TabPanel.ActualWidth > 0)
        {
            _tabsWidthOnTop = TabPanel.ActualWidth;
        }

        var (leftInset, rightInset) = CaptionInsets();
        var content = AppBar.Padding.Left + AppBar.Padding.Right + AppBarColumnGaps
            + BackButton.ActualWidth + _tabsWidthOnTop + ChannelBadge.ActualWidth + CustomizeButton.ActualWidth + SettingsTab.ActualWidth;
        MoveTabs(AppBarLayout.PlaceTabs(AppContent.ActualWidth, content, leftInset + rightInset) == TabsPlacement.Bottom);

        LeftInsetColumn.Width = new GridLength(leftInset);
        RightInsetColumn.Width = new GridLength(rightInset);
    }

    private void MoveTabs(bool toBottom)
    {
        if (_isAtBottom == toBottom)
        {
            return;
        }

        // Back, the palette and Settings stay up in the top bar, away from small thumbs.
        _isAtBottom = toBottom;
        (toBottom ? AppBar : BottomBar).Children.Remove(TabPanel);
        (toBottom ? BottomBar : AppBar).Children.Add(TabPanel);
        Grid.SetColumn(TabPanel, toBottom ? 0 : TabsColumn);
        BottomBar.Visibility = toBottom ? Visibility.Visible : Visibility.Collapsed;

        // At the bottom the tabs are for thumbs: sticker over label, and big. They share the width
        // equally, so they fit the smallest phone without growing silly on a wider one.
        TabPanel.MaxWidth = toBottom ? 420 : double.PositiveInfinity;
        foreach (var column in TabPanel.ColumnDefinitions)
        {
            column.Width = toBottom ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
        }

        foreach (var content in new[] { CountdownTabContent, CalendarTabContent, WishesTabContent })
        {
            content.Orientation = toBottom ? Orientation.Vertical : Orientation.Horizontal;
            content.Spacing = toBottom ? 0 : 4;
        }

        foreach (var tab in new[] { CountdownTab, CalendarTab, WishesTab })
        {
            tab.FontSize = toBottom ? 14 : 16;
            tab.MinWidth = toBottom ? 0 : 72;
            tab.Padding = toBottom ? new Thickness(4, 2, 4, 2) : new Thickness(12, 2, 12, 2);
            tab.MinHeight = toBottom ? 56 : 36;
            tab.CornerRadius = new CornerRadius(toBottom ? 28 : 18);
        }

        MakeRoomForDoorBadge();

#if WINAPPSDK
        // With the tabs gone from the title bar, there is room for it to say whose window it is.
        TitleStrip.Visibility = toBottom ? Visibility.Visible : Visibility.Collapsed;
#endif
    }

#if WINAPPSDK
    private bool _extendsIntoTitleBar;

    // Physical pixels, and they swap sides in a right-to-left layout.
    private (double Left, double Right) CaptionInsets()
    {
        if (!_extendsIntoTitleBar)
        {
            return (0, 0);
        }

        var titleBar = _associatedWindow.AppWindow.TitleBar;
        var scale = XamlRoot.RasterizationScale;
        return (titleBar.LeftInset / scale, titleBar.RightInset / scale);
    }
#else
    private (double Left, double Right) CaptionInsets() => (0, 0);
#endif

    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        // Selection also follows navigation that started elsewhere, which must not navigate a second time.
        if (Enum.TryParse<NavigationSection>((sender as FrameworkElement)?.Tag?.ToString(), out var section)
            && ServiceProvider.GetRequiredService<INavigationService>().CurrentSection != section)
        {
            NavigateToSection(section);
        }
    }

    private void NavigateToSection(NavigationSection section)
    {
        var nav = ServiceProvider.GetRequiredService<INavigationService>();
        switch (section)
        {
            case NavigationSection.Countdown:
                nav.Navigate<CountdownViewModel>();
                break;
            case NavigationSection.Calendar:
                nav.Navigate<CalendarViewModel>();
                break;
            case NavigationSection.Wishes:
                nav.Navigate<WishListViewModel>();
                break;
            case NavigationSection.Settings:
                nav.Navigate<SettingsViewModel>();
                break;
            default:
                throw new NotSupportedException($"Navigation section not supported: {section}");
        }

        // Tabs, not a journey: only pages opened from a section (the grown-ups corner) have a way back.
        nav.ClearBackStack();
        ViewModel.NotifyCanGoBackChanged();
    }

    private void GoBack()
    {
        if (ServiceProvider.GetRequiredService<INavigationService>().GoBack())
        {
            UpdateTabSelection();
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e) => GoBack();

    private void UpdateTabSelection()
    {
        var section = ServiceProvider.GetRequiredService<INavigationService>().CurrentSection.ToString();
        foreach (var tab in new[] { CountdownTab, CalendarTab, WishesTab, SettingsTab })
        {
            tab.IsChecked = (string?)tab.Tag == section;
        }
    }

    #endregion
}
