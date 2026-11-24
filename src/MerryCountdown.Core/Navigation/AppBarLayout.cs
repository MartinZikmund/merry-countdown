namespace MerryCountdown.Core.Navigation;

/// <summary>Where the tabs go on a head whose title bar is ours to fill.</summary>
public static class AppBarLayout
{
    /// <summary>The least empty bar that still makes a usable handle for dragging the window.</summary>
    public const double MinDragWidth = 64;

    /// <summary>False when the tabs would leave nothing to drag by, so they belong under a title strip instead.</summary>
    public static bool SharesRowWithCaptionButtons(double barWidth, double contentWidth, double captionInsets) =>
        barWidth - contentWidth - captionInsets >= MinDragWidth;
}
