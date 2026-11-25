namespace MerryCountdown.Core.Season;

/// <summary>Scene ids match the renderers in MerryCountdown.Scenes.</summary>
public sealed partial record SceneInfo(string Id, bool RequiresFullVersion);

public static class SceneCatalog
{
    /// <summary>The family's photos with nothing painted in front. Chosen like a scene, but not one of <see cref="All"/>.</summary>
    public const string PhotosId = "photos";

    public static IReadOnlyList<SceneInfo> All { get; } =
    [
        new("village", false),
        new("globe", false),
        new("aurora", true),
        new("fireplace", true),
        new("train", true),
    ];

    public static IReadOnlyList<SceneInfo> Available(bool isFull) =>
        isFull ? All : [.. All.Where(s => !s.RequiresFullVersion)];

    public static SceneInfo Resolve(string? id, bool isFull)
    {
        var available = Available(isFull);
        return available.FirstOrDefault(s => s.Id == id) ?? available[0];
    }

    public static SceneInfo Next(string? id, bool isFull)
    {
        var available = Available(isFull);
        var index = available.ToList().IndexOf(Resolve(id, isFull));
        return available[(index + 1) % available.Count];
    }
}
