using Microsoft.UI.Xaml.Media.Imaging;

namespace MerryCountdown.Controls;

/// <summary>Turns a sticker key into its picture. Usable straight from x:Bind.</summary>
public static class Stickers
{
    public static BitmapImage Image(string? key) =>
        new(new Uri($"ms-appx:///Assets/Stickers/{(string.IsNullOrEmpty(key) ? "star" : key)}.png"));
}
