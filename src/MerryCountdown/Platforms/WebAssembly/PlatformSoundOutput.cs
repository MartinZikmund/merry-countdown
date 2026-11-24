using System.Runtime.InteropServices.JavaScript;
using MerryCountdown.Core.Services;

namespace MerryCountdown.Services;

/// <summary>Web Audio: effects are fetched and decoded into buffers up front. See WasmScripts/MerrySounds.js.</summary>
public sealed partial class PlatformSoundOutput : ISoundOutput
{
    private readonly ILogger<PlatformSoundOutput> _logger;

    public PlatformSoundOutput(ILogger<PlatformSoundOutput> logger)
    {
        _logger = logger;

        try
        {
            // The bootstrapper serves the app's files from a versioned folder and says which through these.
            var site = Environment.GetEnvironmentVariable("UNO_BOOTSTRAP_WEBAPP_BASE_PATH") ?? "/";
            var package = Environment.GetEnvironmentVariable("UNO_BOOTSTRAP_APP_BASE") ?? "";
            var folder = $"{site}{package}".TrimEnd('/');

            foreach (var effect in Enum.GetValues<SoundEffect>())
            {
                Load(effect.ToString(), $"{folder}/Assets/Sounds/{SoundFiles.NameOf(effect)}");
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Couldn't set up the sounds");
        }
    }

    public void Play(SoundEffect effect)
    {
        // A missing jingle must never take the app down with it.
        try
        {
            Play(effect.ToString());
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Couldn't play {Effect}", effect);
        }
    }

    [JSImport("globalThis.MerrySounds.load")]
    private static partial void Load(string name, string url);

    [JSImport("globalThis.MerrySounds.play")]
    private static partial void Play(string name);
}
