using MerryCountdown.Core.Services;
using Windows.Media.Core;
using Windows.Media.Playback;
using Player = Windows.Media.Playback.MediaPlayer;

namespace MerryCountdown.Services;

/// <summary>One player per effect, so different sounds overlap and a repeated one restarts.</summary>
public sealed class MediaSoundOutput(ILogger<MediaSoundOutput> logger) : ISoundOutput
{
    private readonly Dictionary<SoundEffect, Player> _players = [];

    public void Play(SoundEffect effect)
    {
        // A missing jingle must never take the app down with it.
        try
        {
            if (!_players.TryGetValue(effect, out var player))
            {
                player = Create(effect);
                _players[effect] = player;
            }

            // Seeking while it's still opening kills the player on Android, and there's nothing to rewind yet.
            if (player.PlaybackSession.PlaybackState != MediaPlaybackState.Opening)
            {
                player.PlaybackSession.Position = TimeSpan.Zero;
            }

            player.Play();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Couldn't play {Effect}", effect);
        }
    }

    private Player Create(SoundEffect effect)
    {
        Player player = new();
        player.MediaFailed += (_, args) => logger.LogWarning("Couldn't play {Effect}: {Error}", effect, args.ErrorMessage);
        player.Source = MediaSource.CreateFromUri(new Uri($"ms-appx:///Assets/Sounds/{SoundFiles.NameOf(effect)}"));

#if WINAPPSDK
        // Keeps a tap sound off the system media overlay and doesn't pause the family's music.
        player.CommandManager.IsEnabled = false;
        player.AudioCategory = MediaPlayerAudioCategory.SoundEffects;
#endif
        return player;
    }
}
