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
    private const double NarrowWidth = 480;

    private readonly IServiceScope _windowScope;
    private readonly Window _associatedWindow;
    private bool _isWindowClosed;
    private ViewModelBase? _currentPageViewModel;
    private readonly IDisplayModeService _display;
    private readonly ICompactOverlayService _compactOverlay;
    private Controls.LookPicker? _lookPicker;

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

        InnerFrame.Navigated += InnerFrame_Navigated;
        _display = ServiceProvider.GetRequiredService<IDisplayModeService>();
        _display.Changed += OnDisplayModeChanged;
        _compactOverlay = ServiceProvider.GetRequiredService<ICompactOverlayService>();
        _compactOverlay.Changed += OnCompactOverlayChanged;
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

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        _isWindowClosed = true;
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
        XamlRoot.Changed += (_, _) => ArrangeTitleBar();
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

    private void AppContent_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var isNarrow = e.NewSize.Width < NarrowWidth;
        var orientation = isNarrow ? Orientation.Vertical : Orientation.Horizontal;

        CountdownTabContent.Orientation = orientation;
        CalendarTabContent.Orientation = orientation;
        WishesTabContent.Orientation = orientation;

        foreach (var tab in new[] { CountdownTab, CalendarTab, WishesTab })
        {
            tab.FontSize = isNarrow ? 14 : 16;
        }

        MakeRoomForDoorBadge();
        ArrangeTitleBar();
    }

    // The badge hangs off the door's corner. Beside the label that's where the label starts, so the label moves over.
    private void MakeRoomForDoorBadge()
    {
        const double BadgeOverhang = 12;
        var isBesideLabel = CalendarTabContent.Orientation == Orientation.Horizontal;
        DoorSticker.Margin = new Thickness(0, 0, isBesideLabel && ViewModel.HasWaitingDoor ? BadgeOverhang : 0, 0);
    }

    private void AppBarContent_SizeChanged(object sender, SizeChangedEventArgs e) => ArrangeTitleBar();

#if WINAPPSDK
    private const double TallCaptionHeight = 48;
    private const double AppBarColumnGaps = 28;

    private bool _extendsIntoTitleBar;

    // Tabs beside the caption buttons while that leaves a handle to drag by, otherwise under a title strip.
    private void ArrangeTitleBar()
    {
        if (!_extendsIntoTitleBar || XamlRoot is null || AppBar.ActualWidth == 0)
        {
            return;
        }

        // Insets are physical pixels, and swap sides in a right-to-left layout.
        var titleBar = _associatedWindow.AppWindow.TitleBar;
        var scale = XamlRoot.RasterizationScale;
        var leftInset = titleBar.LeftInset / scale;
        var rightInset = titleBar.RightInset / scale;

        var content = AppBar.Padding.Left + AppBar.Padding.Right + AppBarColumnGaps
            + BackButton.ActualWidth + TabPanel.ActualWidth + ChannelBadge.ActualWidth + CustomizeButton.ActualWidth + SettingsTab.ActualWidth;
        var sharesRow = AppBarLayout.SharesRowWithCaptionButtons(AppBar.ActualWidth, content, leftInset + rightInset);

        LeftInsetColumn.Width = new GridLength(sharesRow ? leftInset : 0);
        RightInsetColumn.Width = new GridLength(sharesRow ? rightInset : 0);
        TitleStrip.Visibility = sharesRow ? Visibility.Collapsed : Visibility.Visible;
        TitleStrip.Padding = new Thickness(leftInset, 0, rightInset, 0);

        Grid.SetRow(DragRegion, sharesRow ? 1 : 0);
        Grid.SetColumn(DragRegion, sharesRow ? 3 : 0);
        Grid.SetColumnSpan(DragRegion, sharesRow ? 1 : 8);

        // Out over the bar's padding, but never over the tabs: a drag region swallows their clicks.
        var padding = AppBar.Padding;
        DragRegion.Margin = sharesRow
            ? new Thickness(0, -padding.Top, 0, -padding.Bottom)
            : new Thickness(-padding.Left, -padding.Top, -padding.Right, 0);

        // Tall buttons fill the bar they share; standard ones fit the strip.
        var height = sharesRow && AppBar.ActualHeight >= TallCaptionHeight ? TitleBarHeightOption.Tall : TitleBarHeightOption.Standard;
        if (titleBar.PreferredHeightOption != height)
        {
            titleBar.PreferredHeightOption = height;
        }
    }
#else
    private void ArrangeTitleBar()
    {
    }
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
