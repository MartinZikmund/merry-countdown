using System.Text.Json;

namespace MerryCountdown.Core.Family;

public interface IFamilyStore
{
    /// <summary>Never throws. Missing or corrupt data yields a fresh family.</summary>
    FamilyData Load();

    /// <summary>Never throws. A failed write leaves the previous save intact.</summary>
    void Save(FamilyData data);
}

/// <summary>A small JSON file written atomically, so a crash can't cost a child their wish list.</summary>
public sealed class JsonFamilyStore(string directory) : IFamilyStore
{
    private const string FileName = "family.json";

    private readonly string _path = Path.Combine(directory, FileName);
    private readonly string _backupPath = Path.Combine(directory, FileName + ".bak");
    private readonly string _tempPath = Path.Combine(directory, FileName + ".tmp");

    public FamilyData Load() => TryRead(_path) ?? TryRead(_backupPath) ?? new FamilyData();

    public void Save(FamilyData data)
    {
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(_tempPath, JsonSerializer.Serialize(data, FamilyJsonContext.Default.FamilyData));

            if (File.Exists(_path))
            {
                File.Copy(_path, _backupPath, overwrite: true);
            }

            File.Move(_tempPath, _path, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Better to lose the last change than to crash a kid's advent morning.
        }
    }

    private static FamilyData? TryRead(string path)
    {
        try
        {
            return File.Exists(path)
                ? JsonSerializer.Deserialize(File.ReadAllText(path), FamilyJsonContext.Default.FamilyData)
                : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }
}
