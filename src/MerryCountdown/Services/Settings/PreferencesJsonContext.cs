using System.Text.Json.Serialization;

namespace MerryCountdown.Services.Settings;

/// <summary>
/// Source-generated metadata for every type stored with <see cref="Preferences.SetComplex{T}"/>.
/// An unregistered type throws <see cref="NotSupportedException"/> when saved.
/// </summary>
[JsonSerializable(typeof(ElementTheme))]
internal sealed partial class PreferencesJsonContext : JsonSerializerContext
{
}
