using MerryCountdown.Core.Family;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace MerryCountdown.Controls;

/// <summary>Turns a sticker key into its picture. Usable straight from x:Bind.</summary>
public static class Stickers
{
    private const int PhotoThumbnailWidth = 320;

    public static BitmapImage Image(string? key) =>
        new(new Uri($"ms-appx:///Assets/Stickers/{(string.IsNullOrEmpty(key) ? "star" : key)}.png"));

    public static Brush SelectionBrush(bool isSelected) =>
        isSelected
            ? (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"]
            : new SolidColorBrush(Microsoft.UI.Colors.Transparent);

    public static Visibility WhenBaubles(SignDesign design) =>
        design == SignDesign.Baubles ? Visibility.Visible : Visibility.Collapsed;

    public static BitmapImage Photo(byte[]? bytes)
    {
        BitmapImage image = new() { DecodePixelWidth = PhotoThumbnailWidth };
        if (bytes is not null)
        {
            _ = image.SetSourceAsync(new MemoryStream(bytes).AsRandomAccessStream());
        }

        return image;
    }
}
