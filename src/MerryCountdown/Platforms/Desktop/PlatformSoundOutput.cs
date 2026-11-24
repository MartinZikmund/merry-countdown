using MerryCountdown.Core.Services;
using Windows.Media.Core;
using Windows.Media.Playback;
using Player = Windows.Media.Playback.MediaPlayer;

namespace MerryCountdown.Services;

/// <summary>
/// The one head without a sound API of its own: Windows, macOS and Linux share this build, so it stays on Uno's
/// MediaPlayer. One player per effect, opened up front, so different sounds overlap and a tap doesn't wait for a file.
/// </summary>
public sealed class PlatformSoundOutput : ISoundOutput
{
    private readonly ILogger<PlatformSoundOutput> _logger;
    private readonly Dictionary<SoundEffect, Player> _players = [];

    public PlatformSoundOutput(ILogger<PlatformSoundOutput> logger)
    {
        _logger = logger;

        foreach (var effect in Enum.GetValues<SoundEffect>())
        {
            try
            {
                _players[effect] = Create(effect);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Couldn't load {Effect}", effect);
            }
        }
    }

    public void Play(SoundEffect effect)
    {
        // A missing jingle must never take the app down with it.
        try
        {
            if (!_players.TryGetValue(effect, out var player))
            {
                return;
            }

            // There's nothing to rewind while it's still opening.
            if (player.PlaybackSession.PlaybackState != MediaPlaybackState.Opening)
            {
                player.PlaybackSession.Position = TimeSpan.Zero;
            }

            player.Play();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Couldn't play {Effect}", effect);
        }
    }

    private Player Create(SoundEffect effect)
    {
        Player player = new();
        player.MediaFailed += (_, args) => _logger.LogWarning("Couldn't play {Effect}: {Error}", effect, args.ErrorMessage);
        player.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/Sounds/{SoundFiles.NameOf(effect)}"));
        return player;
    }
}
