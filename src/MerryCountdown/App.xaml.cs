using MerryCountdown.Core.Family;
using MerryCountdown.Core.Infrastructure;
using MerryCountdown.Core.Parents;
using MerryCountdown.Core.Store;
using MerryCountdown.Core.Services;
using MerryCountdown.Core.ViewModels;
using MerryCountdown.Infrastructure;
using MerryCountdown.Services.Dialogs;
using MerryCountdown.Services.Navigation;
using MerryCountdown.Services.Rating;
using MerryCountdown.Services.Settings;
using MerryCountdown.Services.Store;
using MerryCountdown.Services.Theming;
using Uno.Resizetizer;

namespace MerryCountdown;

public partial class App : Application, IApplication
{
    public static new App Current => (App)Application.Current;

    public IServiceProvider Services => Host!.Services;

    public string AppVersion
    {
        get
        {
            var version = Windows.ApplicationModel.Package.Current.Id.Version;
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    public string? WorktreeName =>
        string.IsNullOrEmpty(AppEnvironment.WorktreeName) ? null : AppEnvironment.WorktreeName;

    public App()
    {
#if DEBUG
        UnhandledException += (_, e) => File.WriteAllText(
            Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "crash.txt"),
            e.Message + Environment.NewLine + e.Exception);
#endif
        this.InitializeComponent();
    }

    public Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .Configure(host => host
#if DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(ConfigureLogging, enableUnoLogging: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                )
                .UseLocalization()
                .UseDefaultServiceProvider((context, options) =>
                {
                    options.ValidateScopes = true;
                    options.ValidateOnBuild = true;
                })
                .ConfigureServices(RegisterServices)
            );

        MainWindow = builder.Window;

#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();
        SizeMainWindow(MainWindow);

        // On the web the file system loads from IndexedDB asynchronously. Family data can't be read before this.
        await Windows.Storage.ApplicationData.Current.LocalFolder.CreateFolderAsync("Family", Windows.Storage.CreationCollisionOption.OpenIfExists);

        Host = builder.Build();
        IoC.SetProvider(Host.Services);

        // Run app lifecycle updates
        var appPreferences = Host.Services.GetRequiredService<IAppPreferences>();
        var appUpdater = Host.Services.GetRequiredService<IAppUpdater>();
        await appUpdater.EnsureAppUpToDateAsync();
        appPreferences.LaunchCount++;

        // Create WindowShell as root content
        if (MainWindow.Content is not WindowShell)
        {
            var shell = new WindowShell(Host.Services, MainWindow);
            MainWindow.Content = shell;
        }

        MainWindow.Activate();

        _ = Host.Services.GetRequiredService<IFullVersionService>().RefreshAsync();

        if (Host.Services.GetRequiredService<IFamilyService>().Settings.StartInFamilyDisplay || WasAskedForFamilyDisplay(args.Arguments))
        {
            ((WindowShell)MainWindow.Content).EnterFamilyDisplayOnLoad();
        }
    }

    // --display on desktop, ?display=1 on the web: lets a TV or a kiosk start straight in the big screen.
    private static bool WasAskedForFamilyDisplay(string? arguments) =>
        Environment.GetCommandLineArgs().Contains("--display")
        || (arguments?.Contains("display=1", StringComparison.OrdinalIgnoreCase) ?? false);

    private static void RegisterServices(HostBuilderContext context, IServiceCollection services)
    {
        // Singleton services
        services.AddSingleton<IApplication>(sp => Current);
        services.AddSingleton<MerryCountdown.Core.Services.Settings.IPreferences, Preferences>();
        services.AddSingleton<IAppPreferences, AppPreferences>();
        services.AddSingleton<IDisplayRequestManager, DisplayRequestManager>();
        services.AddSingleton<IAppUpdater, Infrastructure.AppUpdater>();
        services.AddScoped<IAppRatingService, AppRatingService>();

        // Family, time and money
        services.AddSingleton<FamilyService>(_ => new FamilyService(new JsonFamilyStore(Windows.Storage.ApplicationData.Current.LocalFolder.Path)));
        services.AddSingleton<IFamilyService>(sp => sp.GetRequiredService<FamilyService>());
        services.AddSingleton<IAppClock, SystemClock>();
        services.AddSingleton<ITicker, UiTicker>();
        services.AddSingleton<IEntitlementCache, FamilyEntitlementCache>();
        services.AddSingleton<IFullVersionService, FullVersionService>();
        RegisterStore(services);

        // Per-window scoped services
        services.AddScoped<KidPickerViewModel>();
        services.AddScoped<IPhotoLibrary, PhotoLibrary>();
        services.AddScoped<PhotoBackdrop>();
        services.AddScoped<IDisplayModeService, DisplayModeService>();
        services.AddScoped<IParentGateService, ParentGateService>();
        services.AddScoped<ISnackService, SnackService>();
        services.AddScoped<IThemeManager, ThemeManager>();
        services.AddScoped<WindowShellProvider>();
        services.AddScoped<IWindowShellProvider>(sp => sp.GetRequiredService<WindowShellProvider>());
        services.AddScoped<IXamlRootProvider>(sp => sp.GetRequiredService<WindowShellProvider>());
        services.AddScoped<IFrameProvider, FrameProvider>();
        services.AddScoped<IDialogCoordinator, DialogCoordinator>();
        services.AddScoped<IDialogService, DialogService>();
        services.AddScoped<IConfirmationDialogService, ConfirmationDialogService>();
        services.AddScoped<ILauncherService, LauncherService>();
        services.AddScoped<IShareService, ShareService>();
        services.AddScoped<INavigationService>(sp =>
        {
            var service = new NavigationService(sp.GetRequiredService<IFrameProvider>());
            service.RegisterView(typeof(Views.CountdownView), typeof(CountdownViewModel));
            service.RegisterView(typeof(Views.CalendarView), typeof(CalendarViewModel));
            service.RegisterView(typeof(Views.WishListView), typeof(WishListViewModel));
            service.RegisterView(typeof(Views.ParentZoneView), typeof(ParentZoneViewModel));
            service.RegisterView(typeof(Views.SettingsView), typeof(SettingsViewModel));
            return service;
        });

        // Scoped ViewModels
        services.AddScoped<WindowShellViewModel>();

        // Transient ViewModels (new instance per navigation)
        services.AddTransient<CountdownViewModel>();
        services.AddTransient<CalendarViewModel>();
        services.AddTransient<WishListViewModel>();
        services.AddTransient<ParentZoneViewModel>();
        services.AddTransient<DisplayViewModel>();
        services.AddTransient<SettingsViewModel>();
    }

    private static void SizeMainWindow(Window window)
    {
#if WINAPPSDK
        // WinUI has no SizeToContent, and its default is silly on an ultrawide. AppWindow takes physical pixels.
        const int Width = 1180;
        const int Height = 860;
        var scale = GetDpiForWindow(WinRT.Interop.WindowNative.GetWindowHandle(window)) / 96.0;
        window.AppWindow.Resize(new Windows.Graphics.SizeInt32((int)(Width * scale), (int)(Height * scale)));
#endif
    }

#if WINAPPSDK
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);
#endif

    private static void RegisterStore(IServiceCollection services)
    {
#if DEBUG
        // MERRY_SIMULATE_STORE=1, or a simulate-store.txt in LocalFolder for hosts that do not pass the environment on.
        var simulation = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "simulate-store.txt");
        if (Environment.GetEnvironmentVariable("MERRY_SIMULATE_STORE") == "1" || File.Exists(simulation))
        {
            // A file containing "owned" starts with the purchase already made.
            var startsOwned = File.Exists(simulation) && File.ReadAllText(simulation).Contains("owned", StringComparison.Ordinal);
            services.AddSingleton<IStoreService>(new SimulatedStoreService(startsOwned));
            return;
        }
#endif

#if WINAPPSDK
        services.AddSingleton<IStoreService>(_ => new WindowsStoreService(
            () => WinRT.Interop.WindowNative.GetWindowHandle(Current.MainWindow!)));
#elif __ANDROID__ || __IOS__
        Uno.RevenueCat.RevenueCatExtension.AddRevenueCat(services);
        services.AddSingleton<IStoreService, RevenueCatStoreService>();
#else
        // Web and desktop have no billing, so everything is simply free there.
        services.AddSingleton<IStoreService, NoBillingStoreService>();
#endif
    }

    private static void ConfigureLogging(HostBuilderContext context, ILoggingBuilder logBuilder)
    {
        logBuilder
            .SetMinimumLevel(
                context.HostingEnvironment.IsDevelopment() ?
                    LogLevel.Information :
                    LogLevel.Warning)
            .CoreLogLevel(LogLevel.Warning);
    }
}
