namespace MerryCountdown.Core.Family;

/// <summary>One kind of picture in the wish picker. Its name is the resource <c>StickerGroup_{Key}</c>.</summary>
public sealed partial record StickerGroup(string Key, IReadOnlyList<string> Stickers);

/// <summary>
/// The pictures a kid can wish for, by kind. A saved wish stores the key, so never rename or drop one.
/// Each key needs <c>Assets/Stickers/{key}.svg</c> and <c>Sticker_{key}</c> in every language.
/// </summary>
public static class StickerCatalog
{
    public static IReadOnlyList<StickerGroup> Groups { get; } =
    [
        new("toys", ["teddy", "doll", "car", "train", "plane", "robot", "dinosaur", "unicorn", "bricks", "puzzle", "ball", "drum", "guitar", "crown"]),
        new("play", ["bike", "scooter", "sledge", "skates", "kite", "swimming", "tent", "telescope", "paints", "book"]),
        new("gadgets", ["game", "tablet", "headphones", "camera"]),
        new("wear", ["shoes", "hat", "backpack", "watch", "mitten"]),
        new("animals", ["puppy", "kitten", "horse", "rabbit", "fish", "hamster"]),
        new("treats", ["candy", "cookie", "gingerbread", "cake", "sweetsbox", "gift"]),
    ];

    public static IReadOnlyList<string> Keys { get; } = [.. Groups.SelectMany(g => g.Stickers)];
}
