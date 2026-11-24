using Android.Media;
using MerryCountdown.Core.Services;
using SoundEffect = MerryCountdown.Core.Services.SoundEffect;

namespace MerryCountdown.Services;

/// <summary>SoundPool: Android's own answer for short effects, decoded once and mixed with whatever else is playing.</summary>
public sealed class PlatformSoundOutput : ISoundOutput
{
    private const int MaxStreams = 4;

    private readonly ILogger<PlatformSoundOutput> _logger;
    private readonly Dictionary<SoundEffect, int> _sounds = [];
    private readonly SoundPool? _pool;

    public PlatformSoundOutput(ILogger<PlatformSoundOutput> logger)
    {
        _logger = logger;

        try
        {
            // Game sonification: follows the media volume and doesn't take audio focus from the family's music.
            var attributes = new AudioAttributes.Builder()!
                .SetUsage(AudioUsageKind.Game)!
                .SetContentType(AudioContentType.Sonification)!
                .Build();
            _pool = new SoundPool.Builder()!
                .SetMaxStreams(MaxStreams)!
                .SetAudioAttributes(attributes)!
                .Build();

            var assets = Android.App.Application.Context.Assets!;
            foreach (var effect in Enum.GetValues<SoundEffect>())
            {
                using var file = assets.OpenFd($"Assets/Sounds/{SoundFiles.NameOf(effect)}");
                _sounds[effect] = _pool!.Load(file, 1);
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
            if (_pool is not null && _sounds.TryGetValue(effect, out var sound))
            {
                _pool.Play(sound, 1, 1, 1, 0, 1);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Couldn't play {Effect}", effect);
        }
    }
}
