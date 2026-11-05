using SkiaSharp;

namespace MerryCountdown.Scenes;

/// <summary>Prepares a picked photo for keeping: upright, a sensible size, and always a JPEG.</summary>
public static class PhotoImporter
{
    /// <summary>Enough for a 4K TV at a soft crop, and small enough to keep in browser storage.</summary>
    public const int MaxSide = 2560;

    private const int JpegQuality = 85;

    /// <returns>The prepared JPEG, or null when the bytes aren't a picture this platform can read.</returns>
    public static byte[]? Normalize(byte[] data)
    {
        using SKData encoded = SKData.CreateCopy(data);
        using var codec = SKCodec.Create(encoded);
        if (codec is null)
        {
            return null;
        }

        var origin = codec.EncodedOrigin;
        using var source = SKBitmap.Decode(codec);
        if (source is null)
        {
            return null;
        }

        var isSideways = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
        var uprightWidth = isSideways ? source.Height : source.Width;
        var uprightHeight = isSideways ? source.Width : source.Height;
        var scale = Math.Min(1f, MaxSide / (float)Math.Max(uprightWidth, uprightHeight));
        var width = Math.Max(1, (int)Math.Round(uprightWidth * scale));
        var height = Math.Max(1, (int)Math.Round(uprightHeight * scale));

        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (surface is null)
        {
            return null;
        }

        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Black);
        canvas.Scale(width / (float)uprightWidth, height / (float)uprightHeight);
        canvas.Concat(ToUpright(origin, source.Width, source.Height));

        using var image = SKImage.FromBitmap(source);
        canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        canvas.Flush();

        using var result = surface.Snapshot();
        using var jpeg = result.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
        return jpeg?.ToArray();
    }

    // Phones store pixels as the sensor saw them, plus an EXIF note saying how to turn them.
    private static SKMatrix ToUpright(SKEncodedOrigin origin, int width, int height) => origin switch
    {
        SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, width, 0, 1, 0, 0, 0, 1),
        SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, width, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightTop => new SKMatrix(0, -1, height, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, height, -1, 0, width, 0, 0, 1),
        SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, width, 0, 0, 1),
        _ => SKMatrix.Identity,
    };
}
