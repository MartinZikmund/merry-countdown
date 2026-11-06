using MerryCountdown.Core.Family;
using MerryCountdown.Core.Infrastructure;
using MerryCountdown.Core.Season;
using MerryCountdown.Core.Services;

namespace MerryCountdown.Core.ViewModels;

public partial class WindowShellViewModel : ObservableObject
{
    private const int NoDoor = 0;

    private readonly INavigationService _navigationService;
    private readonly IFamilyService _family;
    private readonly IAppClock _clock;
    private readonly IStringLocalizer _localizer;

    public WindowShellViewModel(INavigationService navigationService, IFamilyService family, IAppClock clock, ITicker ticker, IStringLocalizer localizer)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _family = family;
        _clock = clock;
        _localizer = localizer;

        ticker.Second += (_, _) => UpdateWaitingDoor();
        family.Changed += (_, _) => UpdateWaitingDoor();
        UpdateWaitingDoor();
    }

    [ObservableProperty]
    public partial string Title { get; set; } = "";

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial string LoadingStatusMessage { get; set; } = "";

    public bool CanGoBack => _navigationService.CanGoBack;

    /// <summary>Today's door, while this kid still hasn't opened it. It's what the badge on the Doors tab shows.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasWaitingDoor))]
    [NotifyPropertyChangedFor(nameof(DoorsTabName))]
    public partial int WaitingDoorNumber { get; set; } = NoDoor;

    public bool HasWaitingDoor => WaitingDoorNumber != NoDoor;

    public string DoorsTabName => HasWaitingDoor ? _localizer["NavDoorWaiting", WaitingDoorNumber] : _localizer["NavCalendar"];

    public void NotifyCanGoBackChanged() => OnPropertyChanged(nameof(CanGoBack));

    [RelayCommand]
    public void GoBack()
    {
        _navigationService.GoBack();
        NotifyCanGoBackChanged();
    }

    private void UpdateWaitingDoor()
    {
        var now = _clock.Now;
        WaitingDoorNumber = AdventCalendar.TodaysDoor(now) is { } today
            && !_family.IsDoorOpened(_family.ActiveKid, AdventCalendar.SeasonYear(now), today)
                ? today
                : NoDoor;
    }
}
