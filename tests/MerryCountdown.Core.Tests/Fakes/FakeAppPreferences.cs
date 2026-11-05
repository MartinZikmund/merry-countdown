using MerryCountdown.Services.Settings;
using Microsoft.UI.Xaml;

namespace MerryCountdown.Core.Tests.Fakes;

internal sealed class FakeAppPreferences : IAppPreferences
{
    public int DataVersion { get; set; }

    public bool FirstStart { get; set; }

    public int LaunchCount { get; set; }

    public bool OfferUserRating { get; set; }

    public ElementTheme Theme { get; set; } = ElementTheme.Default;
}
