using AVFoundation;
using Foundation;
using MerryCountdown.Core.Services;

namespace MerryCountdown.Services;

/// <summary>One prepared AVAudioPlayer per effect: buffers are filled up front, so a tap plays straight away.</summary>
public sealed class PlatformSoundOutput : ISoundOutput
{
    private readonly ILogger<PlatformSoundOutput> _logger;
    private readonly Dictionary<SoundEffect, AVAudioPlayer> _players = [];

    public PlatformSoundOutput(ILogger<PlatformSoundOutput> logger)
    {
        _logger = logger;

        try
        {
            // Ambient: obeys the silent switch and plays over the family's music instead of stopping it.
            AVAudioSession.SharedInstance().SetCategory(AVAudioSessionCategory.Ambient);

            foreach (var effect in Enum.GetValues<SoundEffect>())
            {
                var path = Path.Combine(NSBundle.MainBundle.BundlePath, "Assets", "Sounds", SoundFiles.NameOf(effect));
                var player = AVAudioPlayer.FromUrl(NSUrl.FromFilename(path), out var error);
                if (player is null)
                {
                    _logger.LogWarning("Couldn't load {Effect}: {Error}", effect, error?.LocalizedDescription);
                    continue;
                }

                player.PrepareToPlay();
                _players[effect] = player;
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
            if (_players.TryGetValue(effect, out var player))
            {
                // A repeated effect starts over rather than waiting for itself to finish.
                player.CurrentTime = 0;
                player.Play();
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Couldn't play {Effect}", effect);
        }
    }
}
