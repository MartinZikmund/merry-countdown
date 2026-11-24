namespace MerryCountdown.Services;

public interface IDisplayRequestManager
{
    IDisposable RequestActive();

    void Clear();
}
