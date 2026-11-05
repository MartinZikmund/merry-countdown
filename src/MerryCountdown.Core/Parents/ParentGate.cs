using System.Globalization;

namespace MerryCountdown.Core.Parents;

/// <summary>Shown to the grown-up in words, so a child who can read digits still can't guess it.</summary>
public readonly record struct ParentChallenge(int Left, int Right);

/// <summary>Stands in front of purchases and anything that leaves the app, as kids' store policies expect.</summary>
public sealed class ParentGate(Random random)
{
    private const int Smallest = 3;
    private const int Largest = 9;

    public ParentChallenge Next() => new(random.Next(Smallest, Largest + 1), random.Next(Smallest, Largest + 1));

    public static bool Check(ParentChallenge challenge, string? answer) =>
        int.TryParse(answer, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
        && value == challenge.Left * challenge.Right;
}

public interface IParentGateService
{
    /// <summary>True once a grown-up has answered. Backing out or a wrong answer is false.</summary>
    Task<bool> PassAsync();
}
