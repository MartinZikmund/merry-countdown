namespace MerryCountdown.Core.Navigation;

public enum TabsPlacement
{
    Top,
    Bottom,
}

/// <summary>Where the tabs go: in the top bar, which is the title bar on Windows, or in a bar under the thumb.</summary>
public static class AppBarLayout
{
    /// <summary>Narrower than this is a phone held upright, or a window shaped like one.</summary>
    public const double BottomTabsBelow = 640;

    /// <summary>The least empty bar that still makes a usable handle for dragging the window.</summary>
    public const double MinDragWidth = 64;

    /// <summary>False when the tabs would leave nothing to drag the window by.</summary>
    public static bool SharesRowWithCaptionButtons(double barWidth, double contentWidth, double captionInsets) =>
        barWidth - contentWidth - captionInsets >= MinDragWidth;

    /// <param name="contentWidth">Everything in the top bar with the tabs in it.</param>
    public static TabsPlacement PlaceTabs(double barWidth, double contentWidth, double captionInsets) =>
        barWidth >= BottomTabsBelow && SharesRowWithCaptionButtons(barWidth, contentWidth, captionInsets)
            ? TabsPlacement.Top
            : TabsPlacement.Bottom;
}
