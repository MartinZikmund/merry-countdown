using MerryCountdown.Core.Services;

namespace MerryCountdown.Core.Tests.Fakes;

internal sealed class FakePhotoLibrary : IPhotoLibrary
{
    private readonly Dictionary<string, byte[]> _photos = [];

    public string[] NextPick { get; set; } = [];

    public int NextFailures { get; set; }

    public List<string> Deleted { get; } = [];

    public int Loads { get; private set; }

    public void Seed(string id) => _photos[id] = [1, 2, 3];

    public void Lose(string id) => _photos.Remove(id);

    public byte[] Bytes(string id) => _photos[id];

    public byte[]? Load(string id)
    {
        Loads++;
        return _photos.GetValueOrDefault(id);
    }

    public void Delete(string id)
    {
        Deleted.Add(id);
        _photos.Remove(id);
    }

    public Task<PhotoPickResult> PickAndStoreAsync()
    {
        foreach (var id in NextPick)
        {
            Seed(id);
        }

        return Task.FromResult(new PhotoPickResult(NextPick, NextFailures));
    }
}
