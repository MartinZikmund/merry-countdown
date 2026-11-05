using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace MerryCountdown.Controls;

/// <summary>The signs are painted props with their own surfaces, like the scenes, so their colours don't follow the theme.</summary>
internal static class SignColors
{
    public static SolidColorBrush Brush(uint rgb, byte alpha = 0xFF) =>
        new(ColorHelper.FromArgb(alpha, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb));
}
