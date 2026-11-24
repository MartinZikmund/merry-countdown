using SkiaSharp;

namespace MerryCountdown.Scenes;

/// <summary>
/// Holds the scene a canvas paints, and frees the one it replaces. A scene's images live in native memory the GC
/// can't see, so a dropped scene can hold hundreds of megabytes until a full collection happens to run.
/// </summary>
/// <remarks>Some heads paint on a thread of their own: a scene is only freed once no frame is painting it.</remarks>
public sealed class SceneSlot : IDisposable
{
    private readonly Lock _gate = new();
    private Scene? _scene;

    public Scene? Scene => Volatile.Read(ref _scene);

    /// <summary>Puts <paramref name="next"/> in the slot and disposes the scene that was there.</summary>
    public void Replace(Scene? next)
    {
        lock (_gate)
        {
            var previous = _scene;
            Volatile.Write(ref _scene, next);
            if (!ReferenceEquals(previous, next))
            {
                previous?.Dispose();
            }
        }
    }

    public void Render(SKCanvas canvas, int width, int height, float time)
    {
        lock (_gate)
        {
            _scene?.Render(canvas, width, height, time);
        }
    }

    public void Dispose() => Replace(null);
}
