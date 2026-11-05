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

    public static NavigationViewBackButtonVisible BackVisibility(bool canGoBack) =>
        canGoBack ? NavigationViewBackButtonVisible.Visible : NavigationViewBackButtonVisible.Collapsed;

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
        NavView.Visibility = isActive ? Visibility.Collapsed : Visibility.Visible;
    }

    public WindowShellViewModel ViewModel { get; }

    public Frame RootFrame => InnerFrame;

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WindowShellViewModel.Title))
        {
            UpdateWindowTitle();
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
        // Unsubscribe from previous page's ViewModel
        if (_currentPageViewModel is not null)
        {
            _currentPageViewModel.PropertyChanged -= PageViewModel_PropertyChanged;
            _currentPageViewModel = null;
        }

        // Subscribe to new page's ViewModel for title updates
        if (e.Content is FrameworkElement { DataContext: ViewModelBase pageViewModel })
        {
            _currentPageViewModel = pageViewModel;
            _currentPageViewModel.PropertyChanged += PageViewModel_PropertyChanged;
            UpdatePageTitle();
        }
        else
        {
            ViewModel.Title = ServiceProvider.GetRequiredService<IStringLocalizer>()["ApplicationName"];
        }

        ViewModel.NotifyCanGoBackChanged();
        UpdateNavigationViewSelection();
    }

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
    }

    public void SetTitleBar(UIElement? titleBar)
    {
        if (_isWindowClosed)
        {
            return;
        }

#if !HAS_UNO
        _associatedWindow.SetTitleBar(titleBar ?? AppTitleBar);
#else
        _associatedWindow.SetTitleBar(titleBar);
#endif
    }

    private void CustomizeWindow()
    {
#if !HAS_UNO
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            _associatedWindow.ExtendsContentIntoTitleBar = true;
            _associatedWindow.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            _associatedWindow.SetTitleBar(AppTitleBar);
        }
#endif

        if (ApiInformation.IsPropertyPresent("Microsoft.UI.Xaml.Window", "SystemBackdrop"))
        {
            _associatedWindow.SystemBackdrop = new MicaBackdrop();
            Background = null;
        }
    }

    #region Navigation

    private void NavView_Loaded(object sender, RoutedEventArgs e)
        => NavigateToSection(NavigationSection.Countdown);

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer is not NavigationViewItem item)
        {
            return;
        }

        var tag = item.Tag?.ToString();
        if (tag is null && item == (NavigationViewItem)sender.SettingsItem)
        {
            tag = NavigationSection.Settings.ToString();
        }

        // Selection also follows navigation that started elsewhere, which must not navigate a second time.
        if (Enum.TryParse<NavigationSection>(tag, out var section)
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

    private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
    {
        var nav = ServiceProvider.GetRequiredService<INavigationService>();
        if (nav.GoBack())
        {
            UpdateNavigationViewSelection();
        }
    }

#if !HAS_UNO
    private void AppTitleBar_BackRequested(TitleBar sender, object args)
    {
        var nav = ServiceProvider.GetRequiredService<INavigationService>();
        if (nav.GoBack())
        {
            UpdateNavigationViewSelection();
        }
    }
#endif

    private void UpdateNavigationViewSelection()
    {
        var section = ServiceProvider.GetRequiredService<INavigationService>().CurrentSection;
        NavView.SelectedItem = section switch
        {
            NavigationSection.Countdown => CountdownNavItem,
            NavigationSection.Calendar => CalendarNavItem,
            NavigationSection.Wishes => WishesNavItem,
            NavigationSection.Settings => NavView.SettingsItem,
            _ => NavView.SelectedItem,
        };
    }

    #endregion
}
