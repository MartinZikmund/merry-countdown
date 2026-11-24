using MerryCountdown.Services;

namespace MerryCountdown.Core.Tests.Fakes;

internal sealed class FakeDisplayRequests : IDisplayRequestManager
{
    public int Held { get; private set; }

    public int Takes { get; private set; }

    public IDisposable RequestActive()
    {
        Held++;
        Takes++;
        var released = false;
        return new Release(() =>
        {
            if (!released)
            {
                released = true;
                Held--;
            }
        });
    }

    public void Clear() => Held = 0;

    private sealed class Release(Action release) : IDisposable
    {
        public void Dispose() => release();
    }
}
