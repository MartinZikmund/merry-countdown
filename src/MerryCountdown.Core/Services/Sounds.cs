using MerryCountdown.Core.Family;

namespace MerryCountdown.Core.Services;

public enum SoundEffect
{
    DoorOpen,
    DoorLocked,
    Reveal,
    SceneTap,
    Punchline,
    Sticker,
    Letter,
}

public static class SoundFiles
{
    /// <summary>Only letters and underscores: Android packaging renames "-" and Uno's player wouldn't find the file.</summary>
    public static string NameOf(SoundEffect effect) => effect switch
    {
        SoundEffect.DoorOpen => "door_open.mp3",
        SoundEffect.DoorLocked => "door_locked.mp3",
        SoundEffect.Reveal => "reveal.mp3",
        SoundEffect.SceneTap => "scene_tap.mp3",
        SoundEffect.Punchline => "punchline.mp3",
        SoundEffect.Sticker => "sticker.mp3",
        SoundEffect.Letter => "letter.mp3",
        _ => throw new ArgumentOutOfRangeException(nameof(effect)),
    };
}

/// <summary>Whatever makes the noise on this platform.</summary>
public interface ISoundOutput
{
    void Play(SoundEffect effect);
}

public interface ISoundService
{
    void Play(SoundEffect effect);
}

public sealed class SoundService(IFamilyService family, ISoundOutput output) : ISoundService
{
    public void Play(SoundEffect effect)
    {
        if (family.Settings.SoundEnabled)
        {
            output.Play(effect);
        }
    }
}
