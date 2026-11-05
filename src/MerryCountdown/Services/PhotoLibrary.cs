using MerryCountdown.Scenes;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace MerryCountdown.Services;

/// <summary>Copies picked photos into the app's own folder, so they stay on this device and outlive the original.</summary>
public sealed class PhotoLibrary(IWindowShellProvider shells) : IPhotoLibrary
{
    private static string Folder => Path.Combine(ApplicationData.Current.LocalFolder.Path, "Photos");

    public async Task<PhotoPickResult> PickAndStoreAsync()
    {
        FileOpenPicker picker = new()
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            ViewMode = PickerViewMode.Thumbnail,
        };
        foreach (var type in new[] { ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif" })
        {
            picker.FileTypeFilter.Add(type);
        }

#if WINAPPSDK
        WinRT.Interop.InitializeWithWindow.Initialize(picker, WinRT.Interop.WindowNative.GetWindowHandle(shells.Window));
#endif

        var files = await picker.PickMultipleFilesAsync();

        List<string> added = [];
        var failed = 0;
        foreach (var file in files)
        {
            if (await TryStoreAsync(file) is { } id)
            {
                added.Add(id);
            }
            else
            {
                failed++;
            }
        }

        return new PhotoPickResult(added, failed);
    }

    public byte[]? Load(string id)
    {
        try
        {
            var path = PathOf(id);
            return File.Exists(path) ? File.ReadAllBytes(path) : null;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Delete(string id)
    {
        try
        {
            File.Delete(PathOf(id));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // It's already out of the album. A stray file is harmless.
        }
    }

    private static async Task<string?> TryStoreAsync(StorageFile file)
    {
        try
        {
            using var source = await file.OpenStreamForReadAsync();
            using MemoryStream memory = new();
            await source.CopyToAsync(memory);

            if (PhotoImporter.Normalize(memory.ToArray()) is not { } prepared)
            {
                return null;
            }

            var id = Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(Folder);
            await File.WriteAllBytesAsync(PathOf(id), prepared);
            return id;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static string PathOf(string id) => Path.Combine(Folder, id + ".jpg");
}
